using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P8.DisasterRelief;
using TheWatch.P8.DisasterRelief.Relief;
using TheWatch.P8.DisasterRelief.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P8.DisasterRelief.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Geospatial;
using TheWatch.Contracts.MeshNetwork;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

builder.Services.AddWatchCors(builder.Configuration);

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<IDisasterEventService, DisasterEventService>();
builder.Services.AddScoped<IShelterService, ShelterService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, DisasterReliefSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// IGeospatialClient — nearest shelter/evacuation route spatial queries
builder.Services.AddGeospatialClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:Geospatial"] ?? "https+http://geospatial");

// IMeshNetworkClient — broadcast evacuation alerts via mesh network
builder.Services.AddMeshNetworkClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:MeshNetwork"] ?? "https+http://p3-meshnetwork");

builder.AddWatchControllers();

var app = builder.Build();
await app.UseWatchMigrations();

app.UseCors();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new TheWatch.Shared.Security.HangfireDashboardAuthFilter()],
    IsReadOnlyFunc = _ => true
});
app.MapWatchControllers();

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<IResourceService>(
    "resource-matching",
    svc => svc.MatchRequestsToResourcesAsync(100.0),
    "*/5 * * * *"); // Every 5 minutes

RecurringJob.AddOrUpdate<IDisasterEventService>(
    "event-archival",
    svc => svc.ArchiveResolvedEventsAsync(TimeSpan.FromDays(30)),
    "0 4 * * *"); // Daily at 4 AM

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P8.DisasterRelief",
    "P8",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P8.DisasterRelief",
    Program = "P8",
    Name = "DisasterRelief",
    Description = "Evacuation, resource matching, shelters",
    Icon = "cloud_sync",
    Version = "0.2.0"
});

// === Disaster Event Endpoints ===

app.MapPost("/api/events", async (CreateDisasterEventRequest request, IDisasterEventService svc) =>
{
    var evt = await svc.CreateAsync(request);
    return Results.Created($"/api/events/{evt.Id}", evt);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/events", async (IDisasterEventService svc, int? page, int? pageSize, EventStatus? status) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, status);
    return Results.Ok(result);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/events/{id:guid}", async (Guid id, IDisasterEventService svc) =>
{
    var evt = await svc.GetByIdAsync(id);
    return evt is not null ? Results.Ok(evt) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPut("/api/events/{id:guid}/status", async (Guid id, UpdateEventStatusRequest request, IDisasterEventService svc) =>
{
    var evt = await svc.UpdateStatusAsync(id, request);
    return evt is not null ? Results.Ok(evt) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/events/{id:guid}/routes", async (Guid id, IDisasterEventService svc) =>
{
    var routes = await svc.GetRoutesAsync(id);
    return Results.Ok(routes);
}).RequireAuthorization("Authenticated");

app.MapPost("/api/events/{id:guid}/routes", async (Guid id, CreateEvacuationRouteRequest request, IDisasterEventService svc) =>
{
    var route = await svc.AddRouteAsync(request with { DisasterEventId = id });
    return Results.Created($"/api/events/{id}/routes/{route.Id}", route);
}).RequireAuthorization("ResponderAccess");

// === Shelter Endpoints ===

app.MapPost("/api/shelters", async (CreateShelterRequest request, IShelterService svc) =>
{
    var shelter = await svc.CreateAsync(request);
    return Results.Created($"/api/shelters/{shelter.Id}", shelter);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/shelters", async (IShelterService svc, Guid? disasterEventId, ShelterStatus? status) =>
{
    var result = await svc.ListAsync(disasterEventId, status);
    return Results.Ok(result);
});

app.MapGet("/api/shelters/nearby", async (IShelterService svc, double lat, double lon, double? radiusKm) =>
{
    var shelters = await svc.FindNearbyAsync(lat, lon, radiusKm ?? 50.0);
    return Results.Ok(shelters);
});

app.MapPut("/api/shelters/{id:guid}/occupancy", async (Guid id, UpdateOccupancyRequest request, IShelterService svc) =>
{
    var shelter = await svc.UpdateOccupancyAsync(id, request);
    return shelter is not null ? Results.Ok(shelter) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

// === Resource Endpoints ===

app.MapPost("/api/resources/donate", async (DonateResourceRequest request, IResourceService svc) =>
{
    var item = await svc.DonateAsync(request);
    return Results.Created($"/api/resources/{item.Id}", item);
});

app.MapPost("/api/resources/request", async (CreateResourceRequestRecord request, IResourceService svc) =>
{
    var req = await svc.RequestAsync(request);
    return Results.Created($"/api/resources/requests/{req.Id}", req);
});

app.MapGet("/api/resources", async (IResourceService svc, ResourceCategory? category, Guid? disasterEventId) =>
{
    var result = await svc.ListResourcesAsync(category, disasterEventId);
    return Results.Ok(result);
});

app.MapGet("/api/resources/requests", async (IResourceService svc, RequestStatus? status, Guid? disasterEventId) =>
{
    var requests = await svc.ListRequestsAsync(status, disasterEventId);
    return Results.Ok(requests);
}).RequireAuthorization("ResponderAccess");

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
