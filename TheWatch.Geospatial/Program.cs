using System.Text.Json.Serialization;
using Hangfire;
using NetTopologySuite.IO.Converters;
using Hangfire.InMemory;
using NetTopologySuite.Geometries;
using Serilog;
using TheWatch.Geospatial;
using TheWatch.Geospatial.Services;
using TheWatch.Geospatial.Spatial;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new GeoJsonConverterFactory());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Configurable: PostGIS (default) or Azure Maps (when Azure:UseAzureMaps = true)
builder.Services.AddGeospatialProvider(builder.Configuration);
builder.AddWatchSecurity();

var app = builder.Build();

app.UseCors();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.Geospatial", "Geo", "Healthy", DateTime.UtcNow));

app.MapGet("/info", () => new
{
    Service = "TheWatch.Geospatial",
    Program = "Geo",
    Name = "Geospatial",
    Description = "PostGIS geospatial engine — nearest-N, zones, geofencing, routes",
    Icon = "map",
    Version = "0.1.0"
});

// === Nearest-N Queries ===

app.MapGet("/api/geo/nearest/responders", async (double lon, double lat, int? count, double? radius, IGeospatialService svc) =>
{
    var results = await svc.FindNearestRespondersAsync(lon, lat, count ?? 10, radius ?? 10000);
    return Results.Ok(results);
});

app.MapGet("/api/geo/nearest/shelters", async (double lon, double lat, int? count, double? radius, IGeospatialService svc) =>
{
    var results = await svc.FindNearestSheltersAsync(lon, lat, count ?? 5, radius ?? 50000);
    return Results.Ok(results);
});

app.MapGet("/api/geo/nearest/pois", async (double lon, double lat, int? count, string? category, double? radius, IGeospatialService svc) =>
{
    var results = await svc.FindNearestPoisAsync(lon, lat, count ?? 10, category, radius ?? 5000);
    return Results.Ok(results);
});

// === Within-Radius Queries ===

app.MapGet("/api/geo/radius/entities", async (double lon, double lat, double radius, string? entityType, IGeospatialService svc) =>
{
    var results = await svc.FindEntitiesWithinRadiusAsync(lon, lat, radius, entityType);
    return Results.Ok(results);
});

app.MapGet("/api/geo/radius/responders", async (double lon, double lat, double radius, IGeospatialService svc) =>
{
    var results = await svc.FindRespondersWithinRadiusAsync(lon, lat, radius);
    return Results.Ok(results);
});

// === Incident Zones ===

app.MapPost("/api/geo/zones/incident", async (CreateIncidentZoneRequest req, IGeospatialService svc) =>
{
    var zone = await svc.CreateIncidentZoneAsync(req.IncidentId, req.IncidentType, req.Longitude, req.Latitude, req.RadiusMeters, req.Severity);
    return Results.Created($"/api/geo/zones/incident/{zone.Id}", zone);
});

app.MapPut("/api/geo/zones/incident/{id:guid}/expand", async (Guid id, ExpandZoneRequest req, IGeospatialService svc) =>
{
    var zone = await svc.ExpandIncidentZoneAsync(id, req.NewRadiusMeters);
    return zone is not null ? Results.Ok(zone) : Results.NotFound();
});

app.MapGet("/api/geo/zones/contains", async (double lon, double lat, Guid zoneId, IGeospatialService svc) =>
{
    var result = await svc.IsPointInZoneAsync(lon, lat, zoneId);
    return Results.Ok(new { Contains = result });
});

// === Disaster Zones ===

app.MapPost("/api/geo/zones/disaster", async (CreateDisasterZoneRequest req, IGeospatialService svc) =>
{
    var coords = req.BoundaryPoints.Select(p => new Coordinate(p.Longitude, p.Latitude)).ToArray();
    var zone = await svc.CreateDisasterZoneAsync(req.DisasterEventId, req.DisasterType, coords, req.CenterLongitude, req.CenterLatitude, req.Severity);
    return Results.Created($"/api/geo/zones/disaster/{zone.Id}", zone);
});

// === Evacuation Routes ===

app.MapPost("/api/geo/routes/evacuation", async (CreateEvacRouteRequest req, IGeospatialService svc) =>
{
    var waypoints = req.Waypoints.Select(p => new Coordinate(p.Longitude, p.Latitude)).ToArray();
    var route = await svc.CreateEvacuationRouteAsync(req.DisasterZoneId, req.Name, waypoints, req.CapacityPersons);
    return Results.Created($"/api/geo/routes/evacuation/{route.Id}", route);
});

// === Dispatch Routes ===

app.MapPost("/api/geo/routes/dispatch", async (CreateDispatchRouteRequest req, IGeospatialService svc) =>
{
    var route = await svc.CalculateDispatchRouteAsync(req.ResponderId, req.IncidentId, req.OriginLon, req.OriginLat, req.DestLon, req.DestLat);
    return Results.Created($"/api/geo/routes/dispatch/{route.Id}", route);
});

// === Tracking ===

app.MapPost("/api/geo/tracking/register", async (RegisterTrackedEntityRequest req, IGeospatialService svc) =>
{
    var entity = await svc.RegisterTrackedEntityAsync(req.EntityType, req.ExternalEntityId, req.DisplayName, req.Longitude, req.Latitude);
    return Results.Created($"/api/geo/tracking/{entity.Id}", entity);
});

app.MapPut("/api/geo/tracking/{id:guid}/location", async (Guid id, UpdateLocationRequest req, IGeospatialService svc) =>
{
    var entity = await svc.UpdateEntityLocationAsync(id, req.Longitude, req.Latitude, req.Speed, req.Heading);
    return entity is not null ? Results.Ok(entity) : Results.NotFound();
});

// === Family Geofencing ===

app.MapPost("/api/geo/geofences/family", async (CreateFamilyGeofenceRequest req, IGeospatialService svc) =>
{
    var fence = await svc.CreateFamilyGeofenceAsync(req.FamilyGroupId, req.Name, req.Longitude, req.Latitude, req.RadiusMeters, req.AlertType);
    return Results.Created($"/api/geo/geofences/{fence.Id}", fence);
});

app.MapPost("/api/geo/geofences/check", async (CheckGeofenceRequest req, IGeospatialService svc) =>
{
    var events = await svc.CheckGeofencesForMemberAsync(req.MemberId, req.FamilyGroupId, req.Longitude, req.Latitude);
    return Results.Ok(events);
});

app.Run();

public partial class Program { }

// ─── Request DTOs ───

public record CreateIncidentZoneRequest(Guid IncidentId, string IncidentType, double Longitude, double Latitude, double RadiusMeters, ZoneSeverity Severity);
public record ExpandZoneRequest(double NewRadiusMeters);
public record CreateDisasterZoneRequest(Guid DisasterEventId, string DisasterType, List<GeoPointDto> BoundaryPoints, double CenterLongitude, double CenterLatitude, ZoneSeverity Severity);
public record CreateEvacRouteRequest(Guid DisasterZoneId, string Name, List<GeoPointDto> Waypoints, int CapacityPersons);
public record CreateDispatchRouteRequest(Guid ResponderId, Guid IncidentId, double OriginLon, double OriginLat, double DestLon, double DestLat);
public record RegisterTrackedEntityRequest(string EntityType, Guid ExternalEntityId, string DisplayName, double Longitude, double Latitude);
public record UpdateLocationRequest(double Longitude, double Latitude, double Speed = 0, double Heading = 0);
public record CreateFamilyGeofenceRequest(Guid FamilyGroupId, string Name, double Longitude, double Latitude, double RadiusMeters, GeofenceAlertType AlertType);
public record CheckGeofenceRequest(Guid MemberId, Guid FamilyGroupId, double Longitude, double Latitude);
public record GeoPointDto(double Longitude, double Latitude);
