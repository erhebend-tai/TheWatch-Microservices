using SQLite;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Data;

/// <summary>
/// Local SQLite database context for offline data storage.
/// Provides caching for user profile, family data, incidents, and vitals.
/// Also manages offline request queue for sync when connectivity is restored.
/// </summary>
public class WatchLocalDbContext
{
    private readonly SQLiteAsyncConnection _database;

    public WatchLocalDbContext(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
    }

    /// <summary>
    /// Initialize database schema on startup
    /// </summary>
    public async Task InitializeAsync()
    {
        await _database.CreateTableAsync<CachedUserProfile>();
        await _database.CreateTableAsync<CachedFamilyGroup>();
        await _database.CreateTableAsync<CachedFamilyMember>();
        await _database.CreateTableAsync<CachedIncident>();
        await _database.CreateTableAsync<CachedCheckIn>();
        await _database.CreateTableAsync<CachedVitalReading>();
        await _database.CreateTableAsync<OfflineQueueItem>();
        await _database.CreateTableAsync<ConflictLog>();
        await _database.CreateTableAsync<CachedLocation>();
    }

    // User Profile Operations
    public async Task<CachedUserProfile?> GetUserProfileAsync(Guid userId)
    {
        return await _database.Table<CachedUserProfile>()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveUserProfileAsync(CachedUserProfile profile)
    {
        profile.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(profile);
    }

    // Family Group Operations
    public async Task<CachedFamilyGroup?> GetFamilyGroupAsync(Guid groupId)
    {
        return await _database.Table<CachedFamilyGroup>()
            .Where(g => g.Id == groupId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveFamilyGroupAsync(CachedFamilyGroup group)
    {
        group.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(group);
    }

    public async Task<List<CachedFamilyMember>> GetFamilyMembersAsync(Guid groupId)
    {
        return await _database.Table<CachedFamilyMember>()
            .Where(m => m.FamilyGroupId == groupId)
            .ToListAsync();
    }

    public async Task SaveFamilyMemberAsync(CachedFamilyMember member)
    {
        member.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(member);
    }

    // Incident Operations
    public async Task<List<CachedIncident>> GetRecentIncidentsAsync(int limit = 50)
    {
        return await _database.Table<CachedIncident>()
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveIncidentAsync(CachedIncident incident)
    {
        incident.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(incident);
    }

    // Check-in Operations
    public async Task<List<CachedCheckIn>> GetMemberCheckInsAsync(Guid memberId, int limit = 20)
    {
        return await _database.Table<CachedCheckIn>()
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveCheckInAsync(CachedCheckIn checkIn)
    {
        checkIn.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(checkIn);
    }

    // Vital Reading Operations
    public async Task<List<CachedVitalReading>> GetMemberVitalsAsync(Guid memberId, VitalType? type = null, int limit = 100)
    {
        var query = _database.Table<CachedVitalReading>()
            .Where(v => v.MemberId == memberId);

        if (type.HasValue)
        {
            query = query.Where(v => v.Type == type.Value);
        }

        return await query.OrderByDescending(v => v.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveVitalReadingAsync(CachedVitalReading vital)
    {
        vital.LastModifiedUtc = DateTime.UtcNow;
        await _database.InsertOrReplaceAsync(vital);
    }

    // Offline Queue Operations
    public async Task EnqueueRequestAsync(OfflineQueueItem item)
    {
        await _database.InsertAsync(item);
    }

    public async Task<List<OfflineQueueItem>> GetPendingRequestsAsync()
    {
        return await _database.Table<OfflineQueueItem>()
            .Where(q => q.Status == QueueItemStatus.Pending)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateQueueItemAsync(OfflineQueueItem item)
    {
        await _database.UpdateAsync(item);
    }

    public async Task DeleteQueueItemAsync(Guid id)
    {
        await _database.DeleteAsync<OfflineQueueItem>(id);
    }

    // Conflict Log Operations
    public async Task LogConflictAsync(ConflictLog conflict)
    {
        await _database.InsertAsync(conflict);
    }

    public async Task<List<ConflictLog>> GetConflictHistoryAsync(int limit = 50)
    {
        return await _database.Table<ConflictLog>()
            .OrderByDescending(c => c.ResolvedAt)
            .Take(limit)
            .ToListAsync();
    }

    // Location Tracking Operations
    public async Task SaveLocationAsync(CachedLocation location)
    {
        await _database.InsertAsync(location);
    }

    public async Task<List<CachedLocation>> GetUnuploadedLocationsAsync()
    {
        return await _database.Table<CachedLocation>()
            .Where(l => !l.Uploaded)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
    }

    public async Task MarkLocationsUploadedAsync(List<Guid> locationIds)
    {
        foreach (var id in locationIds)
        {
            var location = await _database.GetAsync<CachedLocation>(id);
            location.Uploaded = true;
            await _database.UpdateAsync(location);
        }
    }

    public async Task CloseAsync()
    {
        await _database.CloseAsync();
    }
}

// Entity Models for Local Caching

[Table("CachedUserProfiles")]
public class CachedUserProfile
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string RolesJson { get; set; } = "[]"; // JSON serialized string array
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

[Table("CachedFamilyGroups")]
public class CachedFamilyGroup
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
}

[Table("CachedFamilyMembers")]
public class CachedFamilyMember
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public FamilyRole Role { get; set; }
    public Guid FamilyGroupId { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

[Table("CachedIncidents")]
public class CachedIncident
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public EmergencyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public int Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

[Table("CachedCheckIns")]
public class CachedCheckIn
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public CheckInStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

[Table("CachedVitalReadings")]
public class CachedVitalReading
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public VitalType Type { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

[Table("OfflineQueueItems")]
public class OfflineQueueItem
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty; // GET, POST, PUT, DELETE
    public string Url { get; set; } = string.Empty;
    public string? JsonBody { get; set; }
    public string? Headers { get; set; } // JSON serialized headers
    public int Priority { get; set; } = 5; // 1 = highest, 10 = lowest
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 5;
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

public enum QueueItemStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    DeadLetter
}

[Table("ConflictLogs")]
public class ConflictLog
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // "UserProfile", "FamilyGroup", etc.
    public Guid EntityId { get; set; }
    public string ConflictType { get; set; } = string.Empty; // "ServerWins", "LocalWins", "Merge"
    public string LocalValueJson { get; set; } = string.Empty;
    public string ServerValueJson { get; set; } = string.Empty;
    public string ResolutionJson { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}

[Table("CachedLocations")]
public class CachedLocation
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Uploaded { get; set; } = false;
}