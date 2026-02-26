using System.Collections.Concurrent;
using TheWatch.P1.CoreGateway.Core;

namespace TheWatch.P1.CoreGateway.Services;

public interface IProfileService
{
    Task<UserProfile> CreateAsync(CreateProfileRequest request);
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<ProfileListResponse> ListAsync(int page = 1, int pageSize = 20, UserRole? role = null);
    Task<UserProfile?> UpdateAsync(Guid id, UpdateProfileRequest request);
    Task<UserProfile?> SetPreferenceAsync(Guid id, SetPreferenceRequest request);
    Task<bool> DeactivateAsync(Guid id);
}

public class ProfileService : IProfileService
{
    private readonly ConcurrentDictionary<Guid, UserProfile> _profiles = new();

    public Task<UserProfile> CreateAsync(CreateProfileRequest request)
    {
        var profile = new UserProfile
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role
        };

        _profiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<UserProfile?> GetByIdAsync(Guid id)
    {
        _profiles.TryGetValue(id, out var profile);
        return Task.FromResult(profile);
    }

    public Task<ProfileListResponse> ListAsync(int page, int pageSize, UserRole? role)
    {
        var query = _profiles.Values.Where(p => p.IsActive).AsEnumerable();

        if (role.HasValue)
            query = query.Where(p => p.Role == role.Value);

        var total = query.Count();
        var items = query
            .OrderBy(p => p.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new ProfileListResponse(items, total, page, pageSize));
    }

    public Task<UserProfile?> UpdateAsync(Guid id, UpdateProfileRequest request)
    {
        if (!_profiles.TryGetValue(id, out var profile))
            return Task.FromResult<UserProfile?>(null);

        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName;
        if (request.Phone is not null) profile.Phone = request.Phone;
        if (request.Latitude.HasValue) profile.Latitude = request.Latitude;
        if (request.Longitude.HasValue) profile.Longitude = request.Longitude;
        profile.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<UserProfile?>(profile);
    }

    public Task<UserProfile?> SetPreferenceAsync(Guid id, SetPreferenceRequest request)
    {
        if (!_profiles.TryGetValue(id, out var profile))
            return Task.FromResult<UserProfile?>(null);

        profile.Preferences[request.Key] = request.Value;
        profile.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<UserProfile?>(profile);
    }

    public Task<bool> DeactivateAsync(Guid id)
    {
        if (!_profiles.TryGetValue(id, out var profile))
            return Task.FromResult(false);

        profile.IsActive = false;
        profile.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
