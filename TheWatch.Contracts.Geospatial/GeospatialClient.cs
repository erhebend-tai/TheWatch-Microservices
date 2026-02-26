using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Geospatial.Models;

namespace TheWatch.Contracts.Geospatial;

public class GeospatialClient(HttpClient http) : ServiceClientBase(http, "Geospatial"), IGeospatialClient
{
    public Task<List<NearbyResultDto>> FindNearestRespondersAsync(double longitude, double latitude, int count, double maxRadiusMeters, CancellationToken ct)
        => GetAsync<List<NearbyResultDto>>($"/api/geo/responders/nearby?lon={longitude}&lat={latitude}&count={count}&maxRadius={maxRadiusMeters}", ct);

    public Task<List<NearbyResultDto>> FindNearestSheltersAsync(double longitude, double latitude, int count, double maxRadiusMeters, CancellationToken ct)
        => GetAsync<List<NearbyResultDto>>($"/api/geo/shelters/nearby?lon={longitude}&lat={latitude}&count={count}&maxRadius={maxRadiusMeters}", ct);

    public Task<List<NearbyResultDto>> FindNearestPoisAsync(double longitude, double latitude, int count, string? category, double maxRadiusMeters, CancellationToken ct)
    {
        var query = $"/api/geo/pois/nearby?lon={longitude}&lat={latitude}&count={count}&maxRadius={maxRadiusMeters}";
        if (category is not null) query += $"&category={category}";
        return GetAsync<List<NearbyResultDto>>(query, ct);
    }

    public Task<IncidentZoneDto> CreateIncidentZoneAsync(CreateIncidentZoneRequest request, CancellationToken ct)
        => PostAsync<IncidentZoneDto>("/api/geo/zones/incident", request, ct);

    public Task<IncidentZoneDto> ExpandIncidentZoneAsync(Guid zoneId, ExpandZoneRequest request, CancellationToken ct)
        => PutAsync<IncidentZoneDto>($"/api/geo/zones/{zoneId}/expand", request, ct);

    public Task<bool> IsPointInZoneAsync(double longitude, double latitude, Guid zoneId, CancellationToken ct)
        => GetAsync<bool>($"/api/geo/zones/{zoneId}/contains?lon={longitude}&lat={latitude}", ct);

    public Task<GeoZoneDto> CreateGeofenceAsync(CreateGeofenceRequest request, CancellationToken ct)
        => PostAsync<GeoZoneDto>("/api/geo/geofences", request, ct);

    public Task<TrackedEntityDto> RegisterTrackedEntityAsync(RegisterTrackedEntityRequest request, CancellationToken ct)
        => PostAsync<TrackedEntityDto>("/api/geo/tracking", request, ct);

    public Task<TrackedEntityDto> UpdateEntityLocationAsync(Guid entityId, UpdateEntityLocationRequest request, CancellationToken ct)
        => PutAsync<TrackedEntityDto>($"/api/geo/tracking/{entityId}/location", request, ct);

    public Task<List<GeofenceEventDto>> CheckGeofencesAsync(CheckGeofencesRequest request, CancellationToken ct)
        => PostAsync<List<GeofenceEventDto>>("/api/geo/geofences/check", request, ct);
}
