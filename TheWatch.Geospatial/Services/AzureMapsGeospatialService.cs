using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using TheWatch.Geospatial.Spatial;

namespace TheWatch.Geospatial.Services;

/// <summary>
/// Azure Maps implementation of IGeospatialService.
/// Uses Azure Maps REST APIs for search, routing, and geofencing
/// as an alternative to self-hosted PostGIS.
///
/// Toggle via Azure:UseAzureMaps = true in appsettings.json.
///
/// Differences from PostGIS:
///   - Search/nearest queries: Azure Maps Search API (fuzzy search, POI search, nearby)
///   - Route calculation: Azure Maps Route API (driving/walking directions)
///   - Geofencing: Azure Maps Spatial API (point-in-polygon, geofence triggers)
///   - Tracking: In-memory ConcurrentDictionary (Azure Maps has no built-in state store)
///   - Zones: In-memory storage (polygons stored locally, spatial queries via Azure Maps)
///
/// PostGIS is more powerful for complex spatial queries with full SQL;
/// Azure Maps is better for cloud-native deployments without a PostGIS dependency.
/// </summary>
public class AzureMapsGeospatialService : IGeospatialService
{
    private readonly HttpClient _httpClient;
    private readonly string _subscriptionKey;
    private readonly ILogger<AzureMapsGeospatialService> _logger;
    private readonly GeometryFactory _geometryFactory;

    // In-memory stores (Azure Maps is stateless — we manage entity state here)
    private readonly ConcurrentDictionary<Guid, TrackedEntity> _trackedEntities = new();
    private readonly ConcurrentDictionary<Guid, LocationHistory> _locationHistory = new();
    private readonly ConcurrentDictionary<Guid, IncidentZone> _incidentZones = new();
    private readonly ConcurrentDictionary<Guid, DisasterZone> _disasterZones = new();
    private readonly ConcurrentDictionary<Guid, FamilyGeofence> _familyGeofences = new();
    private readonly ConcurrentDictionary<Guid, EvacuationRoute> _evacuationRoutes = new();
    private readonly ConcurrentDictionary<Guid, ResponderPosition> _responderPositions = new();
    private readonly ConcurrentDictionary<Guid, ShelterLocation> _shelterLocations = new();

    private const string BaseUrl = "https://atlas.microsoft.com";

    public AzureMapsGeospatialService(
        HttpClient httpClient,
        string subscriptionKey,
        ILogger<AzureMapsGeospatialService> logger)
    {
        _httpClient = httpClient;
        _subscriptionKey = subscriptionKey;
        _logger = logger;
        _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: 4326);
    }

    // ─── Nearest-N Queries (Azure Maps Nearby Search) ───

    public async Task<List<NearbyResult>> FindNearestRespondersAsync(
        double longitude, double latitude, int count, double maxRadiusMeters = 10000)
    {
        // Use in-memory responder positions with Haversine distance
        var results = _responderPositions.Values
            .Select(r => new NearbyResult(
                r.ResponderId, "Responder", $"Responder-{r.ResponderId:N8}",
                r.Location.X, r.Location.Y,
                HaversineDistance(latitude, longitude, r.Location.Y, r.Location.X)))
            .Where(r => r.DistanceMeters <= maxRadiusMeters)
            .OrderBy(r => r.DistanceMeters)
            .Take(count)
            .ToList();

        _logger.LogDebug("FindNearestResponders: found {Count} within {Radius}m", results.Count, maxRadiusMeters);
        return await Task.FromResult(results);
    }

    public async Task<List<NearbyResult>> FindNearestSheltersAsync(
        double longitude, double latitude, int count, double maxRadiusMeters = 50000)
    {
        var results = _shelterLocations.Values
            .Select(s => new NearbyResult(
                s.Id, "Shelter", s.Name,
                s.Location.X, s.Location.Y,
                HaversineDistance(latitude, longitude, s.Location.Y, s.Location.X)))
            .Where(r => r.DistanceMeters <= maxRadiusMeters)
            .OrderBy(r => r.DistanceMeters)
            .Take(count)
            .ToList();

        return await Task.FromResult(results);
    }

    public async Task<List<NearbyResult>> FindNearestPoisAsync(
        double longitude, double latitude, int count, string? category = null, double maxRadiusMeters = 5000)
    {
        // Azure Maps Nearby Search API
        try
        {
            var url = $"{BaseUrl}/search/nearby/json" +
                $"?api-version=1.0&subscription-key={_subscriptionKey}" +
                $"&lat={latitude}&lon={longitude}&radius={maxRadiusMeters}&limit={count}";

            if (!string.IsNullOrEmpty(category))
                url += $"&categorySet={MapCategoryToAzureMaps(category)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure Maps Nearby Search failed: {Status}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var results = new List<NearbyResult>();

            if (json.TryGetProperty("results", out var resultsArray))
            {
                foreach (var result in resultsArray.EnumerateArray().Take(count))
                {
                    var pos = result.GetProperty("position");
                    var lat = pos.GetProperty("lat").GetDouble();
                    var lon = pos.GetProperty("lon").GetDouble();
                    var dist = result.TryGetProperty("dist", out var d) ? d.GetDouble() : 0;
                    var name = result.TryGetProperty("poi", out var poi) && poi.TryGetProperty("name", out var n)
                        ? n.GetString() ?? "Unknown" : "Unknown";

                    results.Add(new NearbyResult(
                        Guid.NewGuid(), "POI", name, lon, lat, dist));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Maps Nearby Search failed");
            return [];
        }
    }

    // ─── Within-Radius Queries ───

    public Task<List<NearbyResult>> FindEntitiesWithinRadiusAsync(
        double longitude, double latitude, double radiusMeters, string? entityType = null)
    {
        var allEntities = _trackedEntities.Values
            .Where(e => entityType == null || e.EntityType == entityType)
            .Select(e => new NearbyResult(
                e.ExternalEntityId, e.EntityType, e.DisplayName,
                e.LastKnownLocation.X, e.LastKnownLocation.Y,
                HaversineDistance(latitude, longitude, e.LastKnownLocation.Y, e.LastKnownLocation.X)))
            .Where(r => r.DistanceMeters <= radiusMeters)
            .OrderBy(r => r.DistanceMeters)
            .ToList();

        return Task.FromResult(allEntities);
    }

    public Task<List<ResponderPosition>> FindRespondersWithinRadiusAsync(
        double longitude, double latitude, double radiusMeters)
    {
        var results = _responderPositions.Values
            .Where(r => HaversineDistance(latitude, longitude, r.Location.Y, r.Location.X) <= radiusMeters)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<List<ShelterLocation>> FindSheltersWithinRadiusAsync(
        double longitude, double latitude, double radiusMeters)
    {
        var results = _shelterLocations.Values
            .Where(s => HaversineDistance(latitude, longitude, s.Location.Y, s.Location.X) <= radiusMeters)
            .ToList();

        return Task.FromResult(results);
    }

    // ─── Zone Management ───

    public Task<IncidentZone> CreateIncidentZoneAsync(
        Guid incidentId, string incidentType, double longitude, double latitude,
        double radiusMeters, ZoneSeverity severity)
    {
        var center = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        var boundary = (Polygon)center.Buffer(radiusMeters / 111320.0);

        var zone = new IncidentZone
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            IncidentType = incidentType,
            EpicenterLocation = center,
            PerimeterBoundary = boundary,
            InitialRadiusMeters = radiusMeters,
            CurrentRadiusMeters = radiusMeters,
            Severity = severity
        };

        _incidentZones[zone.Id] = zone;
        _logger.LogInformation("Created incident zone {ZoneId} for incident {IncidentId}", zone.Id, incidentId);
        return Task.FromResult(zone);
    }

    public Task<IncidentZone?> ExpandIncidentZoneAsync(Guid zoneId, double newRadiusMeters)
    {
        if (!_incidentZones.TryGetValue(zoneId, out var zone))
            return Task.FromResult<IncidentZone?>(null);

        var newBoundary = (Polygon)zone.EpicenterLocation.Buffer(newRadiusMeters / 111320.0);
        zone.PerimeterBoundary = newBoundary;
        zone.CurrentRadiusMeters = newRadiusMeters;
        return Task.FromResult<IncidentZone?>(zone);
    }

    public Task<DisasterZone> CreateDisasterZoneAsync(
        Guid disasterEventId, string disasterType, Coordinate[] boundaryCoords,
        double longitude, double latitude, ZoneSeverity severity)
    {
        var ring = new LinearRing(boundaryCoords.Append(boundaryCoords[0]).ToArray());
        var polygon = _geometryFactory.CreatePolygon(ring);
        var center = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        var zone = new DisasterZone
        {
            Id = Guid.NewGuid(),
            DisasterEventId = disasterEventId,
            DisasterType = disasterType,
            AffectedArea = _geometryFactory.CreateMultiPolygon([polygon]),
            Epicenter = center,
            Severity = severity
        };

        _disasterZones[zone.Id] = zone;
        return Task.FromResult(zone);
    }

    public Task<FamilyGeofence> CreateFamilyGeofenceAsync(
        Guid familyGroupId, string name, double longitude, double latitude,
        double radiusMeters, GeofenceAlertType alertType)
    {
        var center = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        var boundary = (Polygon)center.Buffer(radiusMeters / 111320.0);

        var fence = new FamilyGeofence
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyGroupId,
            Name = name,
            Boundary = boundary,
            Center = center,
            RadiusMeters = radiusMeters,
            AlertType = alertType
        };

        _familyGeofences[fence.Id] = fence;
        return Task.FromResult(fence);
    }

    public Task<bool> IsPointInZoneAsync(double longitude, double latitude, Guid zoneId)
    {
        var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

        if (_incidentZones.TryGetValue(zoneId, out var incidentZone))
            return Task.FromResult(incidentZone.PerimeterBoundary.Contains(point));

        if (_disasterZones.TryGetValue(zoneId, out var disasterZone))
            return Task.FromResult(disasterZone.AffectedArea.Contains(point));

        return Task.FromResult(false);
    }

    // ─── Route Calculation (Azure Maps Route API) ───

    public async Task<EvacuationRoute> CreateEvacuationRouteAsync(
        Guid disasterZoneId, string name, Coordinate[] waypoints, int capacityPersons)
    {
        var path = _geometryFactory.CreateLineString(waypoints);
        var distanceMeters = CalculateLineDistanceMeters(waypoints);

        // Try Azure Maps Route API for actual road-based routing
        var estimatedMinutes = await GetRouteEstimateAsync(waypoints) ?? (distanceMeters / 1000.0 * 1.5);

        var route = new EvacuationRoute
        {
            Id = Guid.NewGuid(),
            DisasterZoneId = disasterZoneId,
            RouteName = name,
            Path = path,
            StartPoint = _geometryFactory.CreatePoint(waypoints.First()),
            EndPoint = _geometryFactory.CreatePoint(waypoints.Last()),
            DistanceMeters = distanceMeters,
            EstimatedMinutes = estimatedMinutes,
            CapacityPersons = capacityPersons
        };

        _evacuationRoutes[route.Id] = route;
        return route;
    }

    public async Task<DispatchRoute> CalculateDispatchRouteAsync(
        Guid responderId, Guid incidentId,
        double originLon, double originLat, double destLon, double destLat)
    {
        var origin = _geometryFactory.CreatePoint(new Coordinate(originLon, originLat));
        var dest = _geometryFactory.CreatePoint(new Coordinate(destLon, destLat));

        var waypoints = new[] { new Coordinate(originLon, originLat), new Coordinate(destLon, destLat) };
        var straightLineDistance = HaversineDistance(originLat, originLon, destLat, destLon);
        var estimatedMinutes = await GetRouteEstimateAsync(waypoints) ?? (straightLineDistance / 1000.0 * 1.2);

        var route = new DispatchRoute
        {
            Id = Guid.NewGuid(),
            ResponderId = responderId,
            IncidentId = incidentId,
            Origin = origin,
            Destination = dest,
            RoutePath = _geometryFactory.CreateLineString(waypoints),
            DistanceMeters = straightLineDistance,
            EstimatedMinutes = estimatedMinutes,
            RouteStatus = DispatchRouteStatus.Planned
        };

        return route;
    }

    // ─── Tracking ───

    public Task<TrackedEntity> RegisterTrackedEntityAsync(
        string entityType, Guid externalEntityId, string displayName,
        double longitude, double latitude)
    {
        var entity = new TrackedEntity
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            ExternalEntityId = externalEntityId,
            DisplayName = displayName,
            LastKnownLocation = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
            Status = TrackingStatus.Active,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        _trackedEntities[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<TrackedEntity?> UpdateEntityLocationAsync(
        Guid trackedEntityId, double longitude, double latitude,
        double speed = 0, double heading = 0)
    {
        if (!_trackedEntities.TryGetValue(trackedEntityId, out var entity))
            return Task.FromResult<TrackedEntity?>(null);

        entity.LastKnownLocation = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        entity.LastSpeed = speed;
        entity.LastHeading = heading;
        entity.LastUpdatedAt = DateTimeOffset.UtcNow;

        return Task.FromResult<TrackedEntity?>(entity);
    }

    public Task<LocationHistory> RecordLocationHistoryAsync(
        Guid trackedEntityId, double longitude, double latitude,
        double speed, double heading, double accuracy)
    {
        var history = new LocationHistory
        {
            Id = Guid.NewGuid(),
            TrackedEntityId = trackedEntityId,
            Location = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
            Speed = speed,
            Heading = heading,
            Accuracy = accuracy,
            RecordedAt = DateTimeOffset.UtcNow
        };

        _locationHistory[history.Id] = history;
        return Task.FromResult(history);
    }

    // ─── Geofence Checks ───

    public Task<List<GeofenceEvent>> CheckGeofencesForMemberAsync(
        Guid memberId, Guid familyGroupId, double longitude, double latitude)
    {
        var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        var events = new List<GeofenceEvent>();

        foreach (var fence in _familyGeofences.Values
            .Where(f => f.FamilyGroupId == familyGroupId && f.IsActive))
        {
            var inside = fence.Boundary.Contains(point);
            if (inside && (fence.AlertType == GeofenceAlertType.Entry || fence.AlertType == GeofenceAlertType.Both))
            {
                events.Add(new GeofenceEvent
                {
                    Id = Guid.NewGuid(),
                    FamilyGeofenceId = fence.Id,
                    MemberId = memberId,
                    EventType = GeofenceEventType.Entered,
                    Location = point,
                    OccurredAt = DateTimeOffset.UtcNow
                });
            }
            else if (!inside && (fence.AlertType == GeofenceAlertType.Exit || fence.AlertType == GeofenceAlertType.Both))
            {
                events.Add(new GeofenceEvent
                {
                    Id = Guid.NewGuid(),
                    FamilyGeofenceId = fence.Id,
                    MemberId = memberId,
                    EventType = GeofenceEventType.Exited,
                    Location = point,
                    OccurredAt = DateTimeOffset.UtcNow
                });
            }
        }

        return Task.FromResult(events);
    }

    // ─── Azure Maps API Helpers ───

    private async Task<double?> GetRouteEstimateAsync(Coordinate[] waypoints)
    {
        try
        {
            var coords = string.Join(":", waypoints.Select(w => $"{w.Y},{w.X}"));
            var url = $"{BaseUrl}/route/directions/json" +
                $"?api-version=1.0&subscription-key={_subscriptionKey}" +
                $"&query={coords}&travelMode=car";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("routes", out var routes) &&
                routes.GetArrayLength() > 0)
            {
                var summary = routes[0].GetProperty("summary");
                var travelTimeSeconds = summary.GetProperty("travelTimeInSeconds").GetInt32();
                return travelTimeSeconds / 60.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Maps Route API call failed, using distance estimate");
        }

        return null;
    }

    private static string MapCategoryToAzureMaps(string category) => category.ToLowerInvariant() switch
    {
        "hospital" or "medical" => "7321",
        "police" or "law enforcement" => "7322",
        "fire station" or "fire" => "7392",
        "pharmacy" => "9361",
        "shelter" => "7328",
        "school" => "7372",
        _ => ""
    };

    // ─── Math Helpers ───

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double CalculateLineDistanceMeters(Coordinate[] coords)
    {
        double total = 0;
        for (int i = 1; i < coords.Length; i++)
            total += HaversineDistance(coords[i - 1].Y, coords[i - 1].X, coords[i].Y, coords[i].X);
        return total;
    }
}
