using NetTopologySuite.Geometries;
using TheWatch.Geospatial.Spatial;

namespace TheWatch.Geospatial.Services;

public interface IGeospatialService
{
    // ─── Nearest-N Queries ───
    Task<List<NearbyResult>> FindNearestRespondersAsync(double longitude, double latitude, int count, double maxRadiusMeters = 10000);
    Task<List<NearbyResult>> FindNearestSheltersAsync(double longitude, double latitude, int count, double maxRadiusMeters = 50000);
    Task<List<NearbyResult>> FindNearestPoisAsync(double longitude, double latitude, int count, string? category = null, double maxRadiusMeters = 5000);

    // ─── Within-Radius Queries ───
    Task<List<NearbyResult>> FindEntitiesWithinRadiusAsync(double longitude, double latitude, double radiusMeters, string? entityType = null);
    Task<List<ResponderPosition>> FindRespondersWithinRadiusAsync(double longitude, double latitude, double radiusMeters);
    Task<List<ShelterLocation>> FindSheltersWithinRadiusAsync(double longitude, double latitude, double radiusMeters);

    // ─── Zone / Geofence Management ───
    Task<IncidentZone> CreateIncidentZoneAsync(Guid incidentId, string incidentType, double longitude, double latitude, double radiusMeters, ZoneSeverity severity);
    Task<IncidentZone?> ExpandIncidentZoneAsync(Guid zoneId, double newRadiusMeters);
    Task<DisasterZone> CreateDisasterZoneAsync(Guid disasterEventId, string disasterType, Coordinate[] boundaryCoords, double longitude, double latitude, ZoneSeverity severity);
    Task<FamilyGeofence> CreateFamilyGeofenceAsync(Guid familyGroupId, string name, double longitude, double latitude, double radiusMeters, GeofenceAlertType alertType);
    Task<bool> IsPointInZoneAsync(double longitude, double latitude, Guid zoneId);

    // ─── Route Calculation ───
    Task<EvacuationRoute> CreateEvacuationRouteAsync(Guid disasterZoneId, string name, Coordinate[] waypoints, int capacityPersons);
    Task<DispatchRoute> CalculateDispatchRouteAsync(Guid responderId, Guid incidentId, double originLon, double originLat, double destLon, double destLat);

    // ─── Tracking ───
    Task<TrackedEntity> RegisterTrackedEntityAsync(string entityType, Guid externalEntityId, string displayName, double longitude, double latitude);
    Task<TrackedEntity?> UpdateEntityLocationAsync(Guid trackedEntityId, double longitude, double latitude, double speed = 0, double heading = 0);
    Task<LocationHistory> RecordLocationHistoryAsync(Guid trackedEntityId, double longitude, double latitude, double speed, double heading, double accuracy);

    // ─── Geofence Checks ───
    Task<List<GeofenceEvent>> CheckGeofencesForMemberAsync(Guid memberId, Guid familyGroupId, double longitude, double latitude);
}

public record NearbyResult(Guid EntityId, string EntityType, string Label, double Longitude, double Latitude, double DistanceMeters);
