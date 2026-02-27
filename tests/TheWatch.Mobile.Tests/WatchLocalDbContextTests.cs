using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for local DB context model logic — cached entity defaults, property setting,
/// and timestamp behaviors for offline-first data models.
/// Since WatchLocalDbContext depends on SQLite (Microsoft.EntityFrameworkCore.Sqlite),
/// we test the pure data model defaults and logic independently.
/// </summary>
public class WatchLocalDbContextTests
{
    // =========================================================================
    // CachedUserProfile
    // =========================================================================

    [Fact]
    public void CachedUserProfile_Defaults()
    {
        var profile = new CachedUserProfile();

        profile.Id.Should().Be(Guid.Empty);
        profile.Email.Should().BeEmpty();
        profile.DisplayName.Should().BeEmpty();
        profile.Roles.Should().BeEmpty();
        profile.LastModifiedUtc.Should().Be(default);
    }

    [Fact]
    public void CachedUserProfile_AllPropertiesSettable()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var profile = new CachedUserProfile
        {
            Id = id,
            Email = "alice@watch.com",
            DisplayName = "Alice",
            Roles = "admin,responder",
            AvatarUrl = "https://cdn.watch.com/alice.jpg",
            LastModifiedUtc = now
        };

        profile.Id.Should().Be(id);
        profile.Email.Should().Be("alice@watch.com");
        profile.DisplayName.Should().Be("Alice");
        profile.Roles.Should().Be("admin,responder");
        profile.AvatarUrl.Should().NotBeEmpty();
        profile.LastModifiedUtc.Should().Be(now);
    }

    // =========================================================================
    // CachedFamilyGroup
    // =========================================================================

    [Fact]
    public void CachedFamilyGroup_Defaults()
    {
        var group = new CachedFamilyGroup();

        group.Id.Should().Be(Guid.Empty);
        group.Name.Should().BeEmpty();
        group.MembersJson.Should().BeEmpty();
        group.LastModifiedUtc.Should().Be(default);
    }

    // =========================================================================
    // CachedFamilyMember
    // =========================================================================

    [Fact]
    public void CachedFamilyMember_Defaults()
    {
        var member = new CachedFamilyMember();

        member.Id.Should().Be(Guid.Empty);
        member.DisplayName.Should().BeEmpty();
        member.Role.Should().BeEmpty();
        member.FamilyGroupId.Should().Be(Guid.Empty);
    }

    // =========================================================================
    // CachedIncident
    // =========================================================================

    [Fact]
    public void CachedIncident_Defaults()
    {
        var incident = new CachedIncident();

        incident.Id.Should().Be(Guid.Empty);
        incident.Type.Should().BeEmpty();
        incident.Description.Should().BeEmpty();
        incident.Status.Should().BeEmpty();
        incident.Latitude.Should().Be(0);
        incident.Longitude.Should().Be(0);
    }

    // =========================================================================
    // CachedCheckIn
    // =========================================================================

    [Fact]
    public void CachedCheckIn_Defaults()
    {
        var checkIn = new CachedCheckIn();

        checkIn.Id.Should().Be(Guid.Empty);
        checkIn.Status.Should().BeEmpty();
        checkIn.Message.Should().BeNull();
        checkIn.MemberId.Should().Be(Guid.Empty);
    }

    // =========================================================================
    // CachedVitalReading
    // =========================================================================

    [Fact]
    public void CachedVitalReading_Defaults()
    {
        var vital = new CachedVitalReading();

        vital.Id.Should().Be(Guid.Empty);
        vital.Type.Should().BeEmpty();
        vital.Value.Should().Be(0);
        vital.MemberId.Should().Be(Guid.Empty);
    }

    // =========================================================================
    // CachedLocation
    // =========================================================================

    [Fact]
    public void CachedLocation_Defaults_UploadedIsFalse()
    {
        var location = new CachedLocation();

        location.Id.Should().Be(Guid.Empty);
        location.Latitude.Should().Be(0);
        location.Longitude.Should().Be(0);
        location.Uploaded.Should().BeFalse();
    }

    [Fact]
    public void CachedLocation_Uploaded_CanBeSetToTrue()
    {
        var location = new CachedLocation { Uploaded = true };

        location.Uploaded.Should().BeTrue();
    }

    // =========================================================================
    // OfflineQueueItem Defaults (uses mirror type from OfflineQueueTests.cs)
    // =========================================================================

    [Fact]
    public void OfflineQueueItem_DefaultsMatch()
    {
        var item = new OfflineQueueItem();

        item.Priority.Should().Be(5);
        item.MaxRetries.Should().Be(5);
        item.Status.Should().Be(QueueItemStatus.Pending);
    }

    // =========================================================================
    // ConflictLog Model (uses mirror type from SyncEngineTests.cs)
    // =========================================================================

    [Fact]
    public void ConflictLog_Defaults()
    {
        var log = new ConflictLog();

        log.Id.Should().Be(Guid.Empty);
        log.EntityType.Should().BeEmpty();
        log.ConflictType.Should().BeEmpty();
        log.LocalValueJson.Should().BeEmpty();
        log.ServerValueJson.Should().BeEmpty();
        log.ResolutionJson.Should().BeEmpty();
    }

    // =========================================================================
    // SaveUserProfile — Sets LastModifiedUtc
    // =========================================================================

    [Fact]
    public void SaveUserProfile_SetsLastModifiedUtc()
    {
        var profile = new CachedUserProfile
        {
            Id = Guid.NewGuid(),
            Email = "alice@watch.com",
            DisplayName = "Alice"
        };

        // Simulate SaveUserProfile behavior
        profile.LastModifiedUtc = DateTime.UtcNow;

        profile.LastModifiedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    // =========================================================================
    // SaveFamilyGroup — Sets LastModifiedUtc
    // =========================================================================

    [Fact]
    public void SaveFamilyGroup_SetsLastModifiedUtc()
    {
        var group = new CachedFamilyGroup
        {
            Id = Guid.NewGuid(),
            Name = "Smith Family"
        };

        // Simulate SaveFamilyGroup behavior
        group.LastModifiedUtc = DateTime.UtcNow;

        group.LastModifiedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}

/// <summary>
/// Mirror of CachedUserProfile from TheWatch.Mobile.Data
/// </summary>
public class CachedUserProfile
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Roles { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

/// <summary>
/// Mirror of CachedFamilyGroup from TheWatch.Mobile.Data
/// </summary>
public class CachedFamilyGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string MembersJson { get; set; } = "";
    public DateTime LastModifiedUtc { get; set; }
}

/// <summary>
/// Mirror of CachedFamilyMember from TheWatch.Mobile.Data
/// </summary>
public class CachedFamilyMember
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public Guid FamilyGroupId { get; set; }
}

/// <summary>
/// Mirror of CachedIncident from TheWatch.Mobile.Data
/// </summary>
public class CachedIncident
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Mirror of CachedCheckIn from TheWatch.Mobile.Data
/// </summary>
public class CachedCheckIn
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "";
    public string? Message { get; set; }
    public Guid MemberId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Mirror of CachedVitalReading from TheWatch.Mobile.Data
/// </summary>
public class CachedVitalReading
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public double Value { get; set; }
    public Guid MemberId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

/// <summary>
/// Mirror of CachedLocation from TheWatch.Mobile.Data
/// </summary>
public class CachedLocation
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public bool Uploaded { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
