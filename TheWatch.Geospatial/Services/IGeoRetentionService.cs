namespace TheWatch.Geospatial.Services;

public interface IGeoRetentionService
{
    /// <summary>Purges location history records older than <paramref name="olderThan"/>.</summary>
    /// <returns>Number of records deleted.</returns>
    Task<int> PurgeOldLocationHistoryAsync(TimeSpan olderThan);

    /// <summary>Purges geofence event records older than <paramref name="olderThan"/>.</summary>
    /// <returns>Number of records deleted.</returns>
    Task<int> PurgeOldGeofenceEventsAsync(TimeSpan olderThan);

    /// <summary>Purges family member location records older than <paramref name="olderThan"/>.</summary>
    /// <returns>Number of records deleted.</returns>
    Task<int> PurgeOldFamilyMemberLocationsAsync(TimeSpan olderThan);

    /// <summary>
    /// Purges resolved incident zones that were resolved more than <paramref name="olderThan"/> ago.
    /// </summary>
    /// <returns>Number of records deleted.</returns>
    Task<int> PurgeResolvedIncidentZonesAsync(TimeSpan olderThan);
}
