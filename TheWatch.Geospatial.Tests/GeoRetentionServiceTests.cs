using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using TheWatch.Geospatial.Services;
using TheWatch.Geospatial.Spatial;
using Xunit;

namespace TheWatch.Geospatial.Tests;

public class GeoRetentionServiceTests
{
    private static readonly GeometryFactory Gf =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static GeospatialDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<GeospatialDbContext>()
            .UseInMemoryDatabase($"RetentionTestDb-{Guid.NewGuid()}")
            .Options;
        return new GeospatialDbContext(options);
    }

    // ─── PurgeOldLocationHistoryAsync ───

    [Fact]
    public async Task PurgeOldLocationHistory_DeletesOldRecords()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);
        var entityId = Guid.NewGuid();

        db.LocationHistories.Add(new LocationHistory
        {
            Id = Guid.NewGuid(), TrackedEntityId = entityId,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-40)  // old
        });
        db.LocationHistories.Add(new LocationHistory
        {
            Id = Guid.NewGuid(), TrackedEntityId = entityId,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-5)   // recent
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeOldLocationHistoryAsync(TimeSpan.FromDays(30));

        deleted.Should().Be(1);
        (await db.LocationHistories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PurgeOldLocationHistory_ReturnsZero_WhenNothingIsOld()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);

        db.LocationHistories.Add(new LocationHistory
        {
            Id = Guid.NewGuid(), TrackedEntityId = Guid.NewGuid(),
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeOldLocationHistoryAsync(TimeSpan.FromDays(30));

        deleted.Should().Be(0);
        (await db.LocationHistories.CountAsync()).Should().Be(1);
    }

    // ─── PurgeOldGeofenceEventsAsync ───

    [Fact]
    public async Task PurgeOldGeofenceEvents_DeletesOldRecords()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);

        db.GeofenceEvents.Add(new GeofenceEvent
        {
            Id = Guid.NewGuid(), FamilyGeofenceId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(), EventType = GeofenceEventType.Entered,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            OccurredAt = DateTimeOffset.UtcNow.AddDays(-100) // old
        });
        db.GeofenceEvents.Add(new GeofenceEvent
        {
            Id = Guid.NewGuid(), FamilyGeofenceId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(), EventType = GeofenceEventType.Exited,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            OccurredAt = DateTimeOffset.UtcNow.AddDays(-10)  // recent
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeOldGeofenceEventsAsync(TimeSpan.FromDays(90));

        deleted.Should().Be(1);
        (await db.GeofenceEvents.CountAsync()).Should().Be(1);
    }

    // ─── PurgeOldFamilyMemberLocationsAsync ───

    [Fact]
    public async Task PurgeOldFamilyMemberLocations_DeletesOldRecords()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);
        var familyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        db.FamilyMemberLocations.Add(new FamilyMemberLocation
        {
            Id = Guid.NewGuid(), FamilyGroupId = familyId, MemberId = memberId,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-10)  // old (> 7 days)
        });
        db.FamilyMemberLocations.Add(new FamilyMemberLocation
        {
            Id = Guid.NewGuid(), FamilyGroupId = familyId, MemberId = memberId,
            Location = Gf.CreatePoint(new Coordinate(-97.74, 30.27)),
            RecordedAt = DateTimeOffset.UtcNow.AddDays(-1)   // recent
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeOldFamilyMemberLocationsAsync(TimeSpan.FromDays(7));

        deleted.Should().Be(1);
        (await db.FamilyMemberLocations.CountAsync()).Should().Be(1);
    }

    // ─── PurgeResolvedIncidentZonesAsync ───

    [Fact]
    public async Task PurgeResolvedIncidentZones_DeletesOldResolvedZones()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);
        var center = Gf.CreatePoint(new Coordinate(-97.74, 30.27));

        db.IncidentZones.Add(new IncidentZone
        {
            Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), IncidentType = "Fire",
            EpicenterLocation = center,
            PerimeterBoundary = (Polygon)center.Buffer(0.005),
            InitialRadiusMeters = 500, CurrentRadiusMeters = 500,
            Severity = ZoneSeverity.High,
            IsActive = false,
            ResolvedAt = DateTimeOffset.UtcNow.AddDays(-100) // resolved long ago
        });
        db.IncidentZones.Add(new IncidentZone
        {
            Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), IncidentType = "Flood",
            EpicenterLocation = center,
            PerimeterBoundary = (Polygon)center.Buffer(0.005),
            InitialRadiusMeters = 1000, CurrentRadiusMeters = 1000,
            Severity = ZoneSeverity.Medium,
            IsActive = true  // still active — must NOT be deleted
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeResolvedIncidentZonesAsync(TimeSpan.FromDays(90));

        deleted.Should().Be(1);
        (await db.IncidentZones.CountAsync()).Should().Be(1);
        var remaining = await db.IncidentZones.FirstAsync();
        remaining.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task PurgeResolvedIncidentZones_DoesNotDelete_RecentlyResolvedZones()
    {
        await using var db = CreateDb();
        var svc = new GeoRetentionService(db);
        var center = Gf.CreatePoint(new Coordinate(-97.74, 30.27));

        db.IncidentZones.Add(new IncidentZone
        {
            Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), IncidentType = "Earthquake",
            EpicenterLocation = center,
            PerimeterBoundary = (Polygon)center.Buffer(0.005),
            InitialRadiusMeters = 800, CurrentRadiusMeters = 800,
            Severity = ZoneSeverity.Critical,
            IsActive = false,
            ResolvedAt = DateTimeOffset.UtcNow.AddDays(-30) // resolved 30 days ago — within 90-day window
        });
        await db.SaveChangesAsync();

        var deleted = await svc.PurgeResolvedIncidentZonesAsync(TimeSpan.FromDays(90));

        deleted.Should().Be(0);
        (await db.IncidentZones.CountAsync()).Should().Be(1);
    }
}
