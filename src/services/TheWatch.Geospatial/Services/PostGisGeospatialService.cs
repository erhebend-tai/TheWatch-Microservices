using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using TheWatch.Geospatial.Spatial;

namespace TheWatch.Geospatial.Services;

public class PostGisGeospatialService : IGeospatialService
{
    private readonly GeospatialDbContext _db;
    private readonly GeometryFactory _gf;

    public PostGisGeospatialService(GeospatialDbContext db)
    {
        _db = db;
        _gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    }

    private Point MakePoint(double lon, double lat) => _gf.CreatePoint(new Coordinate(lon, lat));

    // ─── Nearest-N Queries (ST_DWithin + ST_Distance) ───

    public async Task<List<NearbyResult>> FindNearestRespondersAsync(double longitude, double latitude, int count, double maxRadiusMeters = 10000)
    {
        var point = MakePoint(longitude, latitude);
        return await _db.ResponderPositions
            .Where(r => r.DispatchStatus == ResponderDispatchStatus.Available &&
                        r.Location.IsWithinDistance(point, maxRadiusMeters))
            .OrderBy(r => r.Location.Distance(point))
            .Take(count)
            .Select(r => new NearbyResult(
                r.ResponderId, "Responder", $"Responder-{r.ResponderId:N}",
                r.Location.X, r.Location.Y, r.Location.Distance(point)))
            .ToListAsync();
    }

    public async Task<List<NearbyResult>> FindNearestSheltersAsync(double longitude, double latitude, int count, double maxRadiusMeters = 50000)
    {
        var point = MakePoint(longitude, latitude);
        return await _db.ShelterLocations
            .Where(s => s.Status == ShelterStatus.Open &&
                        s.CurrentOccupancy < s.Capacity &&
                        s.Location.IsWithinDistance(point, maxRadiusMeters))
            .OrderBy(s => s.Location.Distance(point))
            .Take(count)
            .Select(s => new NearbyResult(
                s.Id, "Shelter", s.Name,
                s.Location.X, s.Location.Y, s.Location.Distance(point)))
            .ToListAsync();
    }

    public async Task<List<NearbyResult>> FindNearestPoisAsync(double longitude, double latitude, int count, string? category = null, double maxRadiusMeters = 5000)
    {
        var point = MakePoint(longitude, latitude);
        var query = _db.PointOfInterests
            .Where(p => p.Location.IsWithinDistance(point, maxRadiusMeters));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        return await query
            .OrderBy(p => p.Location.Distance(point))
            .Take(count)
            .Select(p => new NearbyResult(
                p.Id, "POI", p.Name,
                p.Location.X, p.Location.Y, p.Location.Distance(point)))
            .ToListAsync();
    }

    // ─── Within-Radius Queries ───

    public async Task<List<NearbyResult>> FindEntitiesWithinRadiusAsync(double longitude, double latitude, double radiusMeters, string? entityType = null)
    {
        var point = MakePoint(longitude, latitude);
        var query = _db.TrackedEntities
            .Where(e => e.Status != TrackingStatus.Offline &&
                        e.LastKnownLocation.IsWithinDistance(point, radiusMeters));

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(e => e.EntityType == entityType);

        return await query
            .OrderBy(e => e.LastKnownLocation.Distance(point))
            .Select(e => new NearbyResult(
                e.ExternalEntityId, e.EntityType, e.DisplayName,
                e.LastKnownLocation.X, e.LastKnownLocation.Y,
                e.LastKnownLocation.Distance(point)))
            .ToListAsync();
    }

    public async Task<List<ResponderPosition>> FindRespondersWithinRadiusAsync(double longitude, double latitude, double radiusMeters)
    {
        var point = MakePoint(longitude, latitude);
        return await _db.ResponderPositions
            .Where(r => r.Location.IsWithinDistance(point, radiusMeters))
            .OrderBy(r => r.Location.Distance(point))
            .ToListAsync();
    }

    public async Task<List<ShelterLocation>> FindSheltersWithinRadiusAsync(double longitude, double latitude, double radiusMeters)
    {
        var point = MakePoint(longitude, latitude);
        return await _db.ShelterLocations
            .Where(s => s.Location.IsWithinDistance(point, radiusMeters))
            .OrderBy(s => s.Location.Distance(point))
            .ToListAsync();
    }

    // ─── Zone / Geofence Management ───

    public async Task<IncidentZone> CreateIncidentZoneAsync(Guid incidentId, string incidentType,
        double longitude, double latitude, double radiusMeters, ZoneSeverity severity)
    {
        var center = MakePoint(longitude, latitude);
        var boundary = (Polygon)center.Buffer(radiusMeters / 111320.0); // approximate degrees
        boundary.SRID = 4326;

        var zone = new IncidentZone
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            IncidentType = incidentType,
            EpicenterLocation = center,
            PerimeterBoundary = boundary,
            InitialRadiusMeters = radiusMeters,
            CurrentRadiusMeters = radiusMeters,
            Severity = severity,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.IncidentZones.Add(zone);
        await _db.SaveChangesAsync();
        return zone;
    }

    public async Task<IncidentZone?> ExpandIncidentZoneAsync(Guid zoneId, double newRadiusMeters)
    {
        var zone = await _db.IncidentZones.FindAsync(zoneId);
        if (zone == null) return null;

        var boundary = (Polygon)zone.EpicenterLocation.Buffer(newRadiusMeters / 111320.0);
        boundary.SRID = 4326;
        zone.PerimeterBoundary = boundary;
        zone.CurrentRadiusMeters = newRadiusMeters;

        await _db.SaveChangesAsync();
        return zone;
    }

    public async Task<DisasterZone> CreateDisasterZoneAsync(Guid disasterEventId, string disasterType,
        Coordinate[] boundaryCoords, double longitude, double latitude, ZoneSeverity severity)
    {
        // Ensure the ring is closed
        var coords = boundaryCoords;
        if (!coords[0].Equals(coords[coords.Length - 1]))
            coords = coords.Append(coords[0]).ToArray();

        var polygon = _gf.CreatePolygon(coords);
        var multiPolygon = _gf.CreateMultiPolygon(new[] { polygon });
        var center = MakePoint(longitude, latitude);

        var zone = new DisasterZone
        {
            Id = Guid.NewGuid(),
            DisasterEventId = disasterEventId,
            DisasterType = disasterType,
            AffectedArea = multiPolygon,
            Epicenter = center,
            Severity = severity,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.DisasterZones.Add(zone);
        await _db.SaveChangesAsync();
        return zone;
    }

    public async Task<FamilyGeofence> CreateFamilyGeofenceAsync(Guid familyGroupId, string name,
        double longitude, double latitude, double radiusMeters, GeofenceAlertType alertType)
    {
        var center = MakePoint(longitude, latitude);
        var boundary = (Polygon)center.Buffer(radiusMeters / 111320.0);
        boundary.SRID = 4326;

        var fence = new FamilyGeofence
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyGroupId,
            Name = name,
            Boundary = boundary,
            Center = center,
            RadiusMeters = radiusMeters,
            AlertType = alertType,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.FamilyGeofences.Add(fence);
        await _db.SaveChangesAsync();
        return fence;
    }

    public async Task<bool> IsPointInZoneAsync(double longitude, double latitude, Guid zoneId)
    {
        var point = MakePoint(longitude, latitude);

        // Check incident zones first
        var incidentZone = await _db.IncidentZones
            .Where(z => z.Id == zoneId && z.PerimeterBoundary.Contains(point))
            .AnyAsync();
        if (incidentZone) return true;

        // Check disaster zones
        var disasterZone = await _db.DisasterZones
            .Where(z => z.Id == zoneId && z.AffectedArea.Contains(point))
            .AnyAsync();
        if (disasterZone) return true;

        // Check geofences
        return await _db.GeoFences
            .Where(f => f.Id == zoneId && f.Boundary.Contains(point))
            .AnyAsync();
    }

    // ─── Route Calculation ───

    public async Task<EvacuationRoute> CreateEvacuationRouteAsync(Guid disasterZoneId, string name,
        Coordinate[] waypoints, int capacityPersons)
    {
        var path = _gf.CreateLineString(waypoints);
        var start = MakePoint(waypoints[0].X, waypoints[0].Y);
        var end = MakePoint(waypoints[waypoints.Length - 1].X, waypoints[waypoints.Length - 1].Y);

        // Approximate distance in meters (Haversine via NTS Length * ~111320)
        var distanceMeters = path.Length * 111320.0;
        var estimatedMinutes = distanceMeters / 1000.0 * 1.5; // ~40 km/h average evacuation speed

        var route = new EvacuationRoute
        {
            Id = Guid.NewGuid(),
            DisasterZoneId = disasterZoneId,
            RouteName = name,
            Path = path,
            StartPoint = start,
            EndPoint = end,
            DistanceMeters = distanceMeters,
            EstimatedMinutes = estimatedMinutes,
            CapacityPersons = capacityPersons,
            RouteStatus = EvacRouteStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.EvacuationRoutes.Add(route);
        await _db.SaveChangesAsync();
        return route;
    }

    public async Task<DispatchRoute> CalculateDispatchRouteAsync(Guid responderId, Guid incidentId,
        double originLon, double originLat, double destLon, double destLat)
    {
        var origin = MakePoint(originLon, originLat);
        var destination = MakePoint(destLon, destLat);
        var path = _gf.CreateLineString(new[] { new Coordinate(originLon, originLat), new Coordinate(destLon, destLat) });

        var distanceMeters = origin.Distance(destination) * 111320.0;
        var estimatedMinutes = distanceMeters / 1000.0 * 1.0; // ~60 km/h for emergency response

        var route = new DispatchRoute
        {
            Id = Guid.NewGuid(),
            ResponderId = responderId,
            IncidentId = incidentId,
            Origin = origin,
            Destination = destination,
            RoutePath = path,
            DistanceMeters = distanceMeters,
            EstimatedMinutes = estimatedMinutes,
            RouteStatus = DispatchRouteStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.DispatchRoutes.Add(route);
        await _db.SaveChangesAsync();
        return route;
    }

    // ─── Tracking ───

    public async Task<TrackedEntity> RegisterTrackedEntityAsync(string entityType, Guid externalEntityId,
        string displayName, double longitude, double latitude)
    {
        var entity = new TrackedEntity
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            ExternalEntityId = externalEntityId,
            DisplayName = displayName,
            LastKnownLocation = MakePoint(longitude, latitude),
            Status = TrackingStatus.Active,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.TrackedEntities.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<TrackedEntity?> UpdateEntityLocationAsync(Guid trackedEntityId,
        double longitude, double latitude, double speed = 0, double heading = 0)
    {
        var entity = await _db.TrackedEntities.FindAsync(trackedEntityId);
        if (entity == null) return null;

        entity.LastKnownLocation = MakePoint(longitude, latitude);
        entity.LastSpeed = speed;
        entity.LastHeading = heading;
        entity.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<LocationHistory> RecordLocationHistoryAsync(Guid trackedEntityId,
        double longitude, double latitude, double speed, double heading, double accuracy)
    {
        var record = new LocationHistory
        {
            Id = Guid.NewGuid(),
            TrackedEntityId = trackedEntityId,
            Location = MakePoint(longitude, latitude),
            Speed = speed,
            Heading = heading,
            Accuracy = accuracy,
            RecordedAt = DateTimeOffset.UtcNow
        };

        _db.LocationHistories.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    // ─── Geofence Checks ───

    public async Task<List<GeofenceEvent>> CheckGeofencesForMemberAsync(Guid memberId, Guid familyGroupId,
        double longitude, double latitude)
    {
        var point = MakePoint(longitude, latitude);
        var events = new List<GeofenceEvent>();

        var activeGeofences = await _db.FamilyGeofences
            .Where(f => f.FamilyGroupId == familyGroupId && f.IsActive)
            .ToListAsync();

        var lastLocation = await _db.FamilyMemberLocations
            .Where(l => l.MemberId == memberId && l.FamilyGroupId == familyGroupId)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefaultAsync();

        foreach (var fence in activeGeofences)
        {
            var isInside = fence.Boundary.Contains(point);
            var wasInside = lastLocation?.IsInsideGeofence == true && lastLocation.ActiveGeofenceId == fence.Id;

            if (isInside && !wasInside && fence.AlertType is GeofenceAlertType.Entry or GeofenceAlertType.Both)
            {
                var ev = new GeofenceEvent
                {
                    Id = Guid.NewGuid(),
                    FamilyGeofenceId = fence.Id,
                    MemberId = memberId,
                    EventType = GeofenceEventType.Entered,
                    Location = point,
                    OccurredAt = DateTimeOffset.UtcNow
                };
                _db.GeofenceEvents.Add(ev);
                events.Add(ev);
            }
            else if (!isInside && wasInside && fence.AlertType is GeofenceAlertType.Exit or GeofenceAlertType.Both)
            {
                var ev = new GeofenceEvent
                {
                    Id = Guid.NewGuid(),
                    FamilyGeofenceId = fence.Id,
                    MemberId = memberId,
                    EventType = GeofenceEventType.Exited,
                    Location = point,
                    OccurredAt = DateTimeOffset.UtcNow
                };
                _db.GeofenceEvents.Add(ev);
                events.Add(ev);
            }
        }

        // Record current location
        var currentInsideFence = activeGeofences.FirstOrDefault(f => f.Boundary.Contains(point));
        var memberLoc = new FamilyMemberLocation
        {
            Id = Guid.NewGuid(),
            FamilyGroupId = familyGroupId,
            MemberId = memberId,
            Location = point,
            Accuracy = 10.0,
            IsInsideGeofence = currentInsideFence != null,
            ActiveGeofenceId = currentInsideFence?.Id,
            RecordedAt = DateTimeOffset.UtcNow
        };
        _db.FamilyMemberLocations.Add(memberLoc);

        if (events.Count > 0 || true) // Always save location update
            await _db.SaveChangesAsync();

        return events;
    }
}
