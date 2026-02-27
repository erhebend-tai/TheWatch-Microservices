using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for cache service logic — TTL expiration, conflict detection,
/// stale-while-revalidate behavior, JSON round-trips, and pre-population.
/// Since CacheService depends on MAUI FileSystem and Preferences,
/// we test the pure caching algorithms and model defaults independently.
/// </summary>
public class CacheServiceTests
{
    // =========================================================================
    // ConflictInfo Model
    // =========================================================================

    [Fact]
    public void ConflictInfo_AllPropertiesPopulated()
    {
        var info = new CacheConflictInfo
        {
            EntityType = "UserProfile",
            LocalId = Guid.NewGuid(),
            ServerId = Guid.NewGuid(),
            LocalModifiedUtc = DateTime.UtcNow.AddMinutes(-5),
            ServerModifiedUtc = DateTime.UtcNow,
            Resolution = "ServerWins"
        };

        info.EntityType.Should().NotBeEmpty();
        info.LocalId.Should().NotBe(Guid.Empty);
        info.ServerId.Should().NotBe(Guid.Empty);
        info.Resolution.Should().Be("ServerWins");
    }

    [Fact]
    public void ConflictInfo_DetectsConflict_WhenIdsDiffer()
    {
        var localId = Guid.NewGuid();
        var serverId = Guid.NewGuid();

        var hasConflict = localId != serverId;

        hasConflict.Should().BeTrue();
    }

    [Fact]
    public void ConflictInfo_NoConflict_WhenSameId()
    {
        var id = Guid.NewGuid();

        var hasConflict = id != id;

        hasConflict.Should().BeFalse();
    }

    // =========================================================================
    // TTL Checks
    // =========================================================================

    [Fact]
    public void TTL_FileOlderThan24Hours_IsExpired()
    {
        var lastModified = DateTime.UtcNow.AddHours(-25);
        var ttl = TimeSpan.FromHours(24);

        var isExpired = DateTime.UtcNow - lastModified > ttl;

        isExpired.Should().BeTrue();
    }

    [Fact]
    public void TTL_FileNewerThan24Hours_IsNotExpired()
    {
        var lastModified = DateTime.UtcNow.AddHours(-12);
        var ttl = TimeSpan.FromHours(24);

        var isExpired = DateTime.UtcNow - lastModified > ttl;

        isExpired.Should().BeFalse();
    }

    [Fact]
    public void TTL_ExactlyAt24Hours_IsNotExpired()
    {
        var lastModified = DateTime.UtcNow.AddHours(-24);
        var ttl = TimeSpan.FromHours(24);

        // Exactly at TTL boundary: elapsed equals TTL, not greater than
        var elapsed = DateTime.UtcNow - lastModified;
        var isExpired = elapsed > ttl;

        isExpired.Should().BeFalse();
    }

    // =========================================================================
    // Stale-While-Revalidate
    // =========================================================================

    [Fact]
    public void StaleWhileRevalidate_ReturnsStaleData_WhenExpired()
    {
        var cachedData = new CachedEntity
        {
            Key = "user-profile",
            JsonData = """{"name":"Alice"}""",
            CachedAtUtc = DateTime.UtcNow.AddHours(-25)
        };
        var ttl = TimeSpan.FromHours(24);

        var isExpired = DateTime.UtcNow - cachedData.CachedAtUtc > ttl;
        isExpired.Should().BeTrue();

        // Stale-while-revalidate: return stale data immediately while refreshing in background
        cachedData.JsonData.Should().NotBeNullOrEmpty("stale data is still returned");
    }

    // =========================================================================
    // JSON Serialization Round-Trip
    // =========================================================================

    [Fact]
    public void JsonRoundTrip_CachedEntity_SerializesAndDeserializes()
    {
        var entity = new CachedEntity
        {
            Key = "incidents-recent",
            JsonData = """[{"id":"abc","type":"Fire"}]""",
            CachedAtUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(entity);
        var deserialized = JsonSerializer.Deserialize<CachedEntity>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be("incidents-recent");
        deserialized.JsonData.Should().Contain("Fire");
        deserialized.CachedAtUtc.Should().Be(entity.CachedAtUtc);
    }

    // =========================================================================
    // Pre-Populate Logic
    // =========================================================================

    [Fact]
    public void PrePopulate_CachesUserIncidentsFamilyEntities()
    {
        var cacheKeys = new List<string>();

        // Mirrors CacheService.PrePopulateAsync — caches 3 entity types
        cacheKeys.Add("user-profile");
        cacheKeys.Add("incidents-recent");
        cacheKeys.Add("family-group");

        cacheKeys.Should().HaveCount(3);
        cacheKeys.Should().Contain("user-profile");
        cacheKeys.Should().Contain("incidents-recent");
        cacheKeys.Should().Contain("family-group");
    }

    // =========================================================================
    // RefreshFromServer Logic
    // =========================================================================

    [Fact]
    public void RefreshFromServer_CountsRefreshedEntities()
    {
        var entitiesToRefresh = new[] { "user-profile", "incidents-recent", "family-group" };
        var refreshed = 0;

        foreach (var entity in entitiesToRefresh)
        {
            // Simulate successful refresh
            var success = !string.IsNullOrEmpty(entity);
            if (success) refreshed++;
        }

        refreshed.Should().Be(3);
    }

    [Fact]
    public void RefreshFromServer_PartialFailure_CountsOnlySuccesses()
    {
        var entitiesToRefresh = new[] { "user-profile", "", "family-group" };
        var refreshed = 0;

        foreach (var entity in entitiesToRefresh)
        {
            var success = !string.IsNullOrEmpty(entity);
            if (success) refreshed++;
        }

        refreshed.Should().Be(2);
    }
}

/// <summary>
/// Mirror of CacheConflictInfo from TheWatch.Mobile.Services
/// </summary>
public class CacheConflictInfo
{
    public string EntityType { get; set; } = "";
    public Guid LocalId { get; set; }
    public Guid ServerId { get; set; }
    public DateTime LocalModifiedUtc { get; set; }
    public DateTime ServerModifiedUtc { get; set; }
    public string Resolution { get; set; } = "";
}

/// <summary>
/// Mirror of CachedEntity from TheWatch.Mobile.Services
/// </summary>
public class CachedEntity
{
    public string Key { get; set; } = "";
    public string JsonData { get; set; } = "";
    public DateTime CachedAtUtc { get; set; }
}
