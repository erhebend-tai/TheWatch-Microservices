using Microsoft.EntityFrameworkCore;

namespace TheWatch.Geospatial.Services;

public class GeoRetentionService : IGeoRetentionService
{
    private readonly GeospatialDbContext _db;

    public GeoRetentionService(GeospatialDbContext db)
    {
        _db = db;
    }

    public async Task<int> PurgeOldLocationHistoryAsync(TimeSpan olderThan)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var toDelete = await _db.LocationHistories
            .Where(l => l.RecordedAt < cutoff)
            .ToListAsync();

        foreach (var record in toDelete)
            _db.LocationHistories.Remove(record);

        if (toDelete.Count > 0)
            await _db.SaveChangesAsync();

        return toDelete.Count;
    }

    public async Task<int> PurgeOldGeofenceEventsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var toDelete = await _db.GeofenceEvents
            .Where(e => e.OccurredAt < cutoff)
            .ToListAsync();

        foreach (var record in toDelete)
            _db.GeofenceEvents.Remove(record);

        if (toDelete.Count > 0)
            await _db.SaveChangesAsync();

        return toDelete.Count;
    }

    public async Task<int> PurgeOldFamilyMemberLocationsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var toDelete = await _db.FamilyMemberLocations
            .Where(l => l.RecordedAt < cutoff)
            .ToListAsync();

        foreach (var record in toDelete)
            _db.FamilyMemberLocations.Remove(record);

        if (toDelete.Count > 0)
            await _db.SaveChangesAsync();

        return toDelete.Count;
    }

    public async Task<int> PurgeResolvedIncidentZonesAsync(TimeSpan olderThan)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var toDelete = await _db.IncidentZones
            .Where(z => !z.IsActive && z.ResolvedAt.HasValue && z.ResolvedAt.Value < cutoff)
            .ToListAsync();

        foreach (var record in toDelete)
            _db.IncidentZones.Remove(record);

        if (toDelete.Count > 0)
            await _db.SaveChangesAsync();

        return toDelete.Count;
    }
}
