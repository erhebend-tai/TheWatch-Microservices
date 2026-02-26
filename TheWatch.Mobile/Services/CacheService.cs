using System.Text.Json;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Local caching service with write-through and stale-while-revalidate pattern.
/// Stores user profile, family data, incidents, and vitals locally for offline access.
/// Uses file-based storage in AppDataDirectory (upgrade to SQLite via WatchLocalDbContext for production).
/// </summary>
public class CacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly WatchApiClient _api;
    private readonly string _cacheDir;
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    internal List<ConflictInfo> RecentConflicts { get; } = [];

    public CacheService(WatchApiClient api, ILogger<CacheService> logger)
    {
        _api = api;
        _logger = logger;
        _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    // --- User Profile ---

    public async Task CacheUserProfileAsync(UserInfoDto profile)
    {
        await WriteAsync("user_profile", profile);
    }

    public async Task<UserInfoDto?> GetCachedUserProfileAsync()
    {
        return await ReadAsync<UserInfoDto>("user_profile");
    }

    // --- Family Group ---

    public async Task CacheFamilyGroupAsync(FamilyGroupDto group)
    {
        await WriteAsync($"family_{group.Id}", group);
    }

    public async Task<FamilyGroupDto?> GetCachedFamilyGroupAsync()
    {
        // Return the most recently cached family group
        var files = Directory.GetFiles(_cacheDir, "family_*.json");
        if (files.Length == 0) return null;

        var mostRecent = files.OrderByDescending(File.GetLastWriteTimeUtc).First();
        var key = Path.GetFileNameWithoutExtension(mostRecent);
        return await ReadAsync<FamilyGroupDto>(key);
    }

    // --- Incidents ---

    public async Task CacheIncidentsAsync(List<IncidentDto> incidents)
    {
        await WriteAsync("recent_incidents", incidents);
    }

    public async Task<List<IncidentDto>> GetCachedIncidentsAsync()
    {
        return await ReadAsync<List<IncidentDto>>("recent_incidents") ?? [];
    }

    // --- Vitals ---

    public async Task CacheVitalsAsync(Guid memberId, List<VitalReadingDto> vitals)
    {
        await WriteAsync($"vitals_{memberId}", vitals);
    }

    public async Task<List<VitalReadingDto>> GetCachedVitalsAsync(Guid memberId)
    {
        return await ReadAsync<List<VitalReadingDto>>($"vitals_{memberId}") ?? [];
    }

    // --- Pre-populate on login ---

    public async Task PrePopulateAsync()
    {
        _logger.LogInformation("Pre-populating cache after login...");
        var tasks = new List<Task>();

        try
        {
            var userTask = Task.Run(async () =>
            {
                var user = await _api.GetCurrentUserAsync();
                if (user is not null) await CacheUserProfileAsync(user);
            });
            tasks.Add(userTask);

            var incidentTask = Task.Run(async () =>
            {
                var incidents = await _api.GetRecentIncidentsAsync(20);
                await CacheIncidentsAsync(incidents);
            });
            tasks.Add(incidentTask);

            var familyTask = Task.Run(async () =>
            {
                var group = await _api.GetFamilyGroupAsync();
                if (group is not null) await CacheFamilyGroupAsync(group);
            });
            tasks.Add(familyTask);

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cache pre-populated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache pre-population (partial data may be cached)");
        }
    }

    // --- Refresh from server (called by SyncEngine) ---

    public async Task<int> RefreshFromServerAsync()
    {
        RecentConflicts.Clear();
        var refreshed = 0;

        try
        {
            // Refresh user profile
            var serverUser = await _api.GetCurrentUserAsync();
            if (serverUser is not null)
            {
                var localUser = await GetCachedUserProfileAsync();
                if (localUser is not null && localUser.Id != serverUser.Id)
                {
                    RecentConflicts.Add(new ConflictInfo("UserProfile", serverUser.Id,
                        "DisplayName", localUser.DisplayName, serverUser.DisplayName));
                }
                await CacheUserProfileAsync(serverUser);
                refreshed++;
            }

            // Refresh incidents
            var incidents = await _api.GetRecentIncidentsAsync(20);
            await CacheIncidentsAsync(incidents);
            refreshed++;

            // Refresh family
            var family = await _api.GetFamilyGroupAsync();
            if (family is not null)
            {
                await CacheFamilyGroupAsync(family);
                refreshed++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing cache from server");
        }

        return refreshed;
    }

    // --- Supporting types ---

    internal record ConflictInfo(string EntityType, Guid EntityId, string Field, string LocalValue, string ServerValue);

    // --- File-based cache helpers ---

    private async Task WriteAsync<T>(string key, T data)
    {
        try
        {
            var filePath = Path.Combine(_cacheDir, $"{key}.json");
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write cache key: {Key}", key);
        }
    }

    private async Task<T?> ReadAsync<T>(string key) where T : class
    {
        try
        {
            var filePath = Path.Combine(_cacheDir, $"{key}.json");
            if (!File.Exists(filePath)) return null;

            // Check TTL
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath) > _ttl)
            {
                _logger.LogDebug("Cache expired for key: {Key}", key);
                // Stale-while-revalidate: return stale data but it's considered expired
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read cache key: {Key}", key);
            return null;
        }
    }
}
