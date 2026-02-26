using TheWatch.Contracts.Geospatial.Models;

namespace TheWatch.Contracts.Geospatial;

public interface IGeospatialClient
{
    Task<List<NearbyResultDto>> FindNearestRespondersAsync(double longitude, double latitude, int count = 10, double maxRadiusMeters = 10000, CancellationToken ct = default);
    Task<List<NearbyResultDto>> FindNearestSheltersAsync(double longitude, double latitude, int count = 10, double maxRadiusMeters = 50000, CancellationToken ct = default);
    Task<List<NearbyResultDto>> FindNearestPoisAsync(double longitude, double latitude, int count = 10, string? category = null, double maxRadiusMeters = 5000, CancellationToken ct = default);
    Task<IncidentZoneDto> CreateIncidentZoneAsync(CreateIncidentZoneRequest request, CancellationToken ct = default);
    Task<IncidentZoneDto> ExpandIncidentZoneAsync(Guid zoneId, ExpandZoneRequest request, CancellationToken ct = default);
    Task<bool> IsPointInZoneAsync(double longitude, double latitude, Guid zoneId, CancellationToken ct = default);
    Task<GeoZoneDto> CreateGeofenceAsync(CreateGeofenceRequest request, CancellationToken ct = default);
    Task<TrackedEntityDto> RegisterTrackedEntityAsync(RegisterTrackedEntityRequest request, CancellationToken ct = default);
    Task<TrackedEntityDto> UpdateEntityLocationAsync(Guid entityId, UpdateEntityLocationRequest request, CancellationToken ct = default);
    Task<List<GeofenceEventDto>> CheckGeofencesAsync(CheckGeofencesRequest request, CancellationToken ct = default);
}
