using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P6.FirstResponder;
using TheWatch.P6.FirstResponder.Responders;
using TheWatch.P6.FirstResponder.Services;
using TheWatch.Shared.Contracts;
using TheWatch.Shared.Events;
using TheWatch.P6.FirstResponder.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.Geospatial;
using TheWatch.Contracts.DisasterRelief;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.AddWatchKafka();
builder.AddWatchKafkaConsumer<IncidentCreatedConsumer>();
builder.AddWatchKafkaConsumer<CrimeLocationReportedConsumer>();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

// CORS (SignalR — requires AllowCredentials)
builder.Services.AddWatchCors(builder.Configuration, requiresSignalR: true);

// SignalR real-time hubs (ResponderHub, CheckInHub)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// ── Inter-service typed HTTP clients (Items 208, 211, 214) ──
// Shared delegating handlers for correlation ID + API key auth (Items 218, 219)
builder.Services.AddWatchClientHandlers();

// Item 208: IVoiceEmergencyClient — look up incidents during dispatch coordination
builder.Services.AddVoiceEmergencyClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:VoiceEmergency"] ?? "https+http://p2-voiceemergency");

// Item 211: IGeospatialClient — spatial queries for responder dispatch (nearest responders, incident zones)
builder.Services.AddGeospatialClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:Geospatial"] ?? "https+http://geospatial");

// Item 214: IDisasterReliefClient — shelter/evacuation coordination during disaster events
builder.Services.AddDisasterReliefClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:DisasterRelief"] ?? "https+http://p8-disasterrelief");

// Services
builder.Services.AddScoped<IResponderService, ResponderService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IDesignatedResponderService, DesignatedResponderService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, FirstResponderSeeder>();
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
RecurringJob.AddOrUpdate<ICheckInService>(
    "checkin-cleanup",
    svc => svc.CleanupOldCheckInsAsync(TimeSpan.FromHours(72)),
    "0 3 * * *"); // Daily at 3 AM

RecurringJob.AddOrUpdate<IDesignatedResponderService>(
    "designated-responder-schedule-sync",
    svc => svc.ActivateScheduledRespondersAsync(),
    "*/5 * * * *"); // Every 5 minutes

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P6.FirstResponder",
    "P6",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P6.FirstResponder",
    Program = "P6",
    Name = "FirstResponder",
    Description = "Responder registration, dispatch, check-in",
    Icon = "local_police",
    Version = "0.2.0"
});

// === Responder Endpoints ===

app.MapPost("/api/responders", async (RegisterResponderRequest request, IResponderService svc) =>
{
    var responder = await svc.RegisterAsync(request);
    return Results.Created($"/api/responders/{responder.Id}", responder);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/responders", async (
    IResponderService svc,
    int? page,
    int? pageSize,
    ResponderType? type,
    ResponderStatus? status) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, type, status);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/responders/{id:guid}", async (Guid id, IResponderService svc) =>
{
    var responder = await svc.GetByIdAsync(id);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapPut("/api/responders/{id:guid}/location", async (Guid id, UpdateLocationRequest request, IResponderService svc, IResponderBroadcaster hub) =>
{
    var responder = await svc.UpdateLocationAsync(id, request);
    if (responder is not null)
    {
        // Broadcast live GPS update to SignalR clients (ResponderLocationHub)
        await hub.BroadcastUpdatedAsync(responder);
    }
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapPut("/api/responders/{id:guid}/status", async (Guid id, UpdateStatusRequest request, IResponderService svc, IResponderBroadcaster hub) =>
{
    var responder = await svc.UpdateStatusAsync(id, request);
    if (responder is not null)
    {
        // Broadcast responder status change in real-time
        await hub.BroadcastUpdatedAsync(responder);
    }
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapDelete("/api/responders/{id:guid}", async (Guid id, IResponderService svc) =>
{
    var ok = await svc.DeactivateAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("AdminOnly");

// === Nearby Search ===

app.MapGet("/api/responders/nearby", async (
    IResponderService svc,
    double lat,
    double lon,
    double? radiusKm,
    ResponderType? type,
    bool? availableOnly) =>
{
    var query = new NearbyResponderQuery(lat, lon, radiusKm ?? 10.0, type, availableOnly ?? true);
    var results = await svc.FindNearbyAsync(query);
    return Results.Ok(results);
}).RequireAuthorization("ResponderAccess");

// === Check-In Endpoints ===

app.MapPost("/api/responders/{responderId:guid}/checkins", async (Guid responderId, CreateCheckInRequest request, ICheckInService svc, ICheckInBroadcaster checkInHub) =>
{
    var checkIn = await svc.CreateAsync(responderId, request);
    // Broadcast check-in to clients watching this incident
    await checkInHub.BroadcastCreatedAsync(checkIn, checkIn.IncidentId.ToString());
    return Results.Created($"/api/responders/{responderId}/checkins/{checkIn.Id}", checkIn);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/responders/{responderId:guid}/checkins", async (Guid responderId, ICheckInService svc) =>
{
    var checkIns = await svc.GetForResponderAsync(responderId);
    return Results.Ok(checkIns);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/incidents/{incidentId:guid}/checkins", async (Guid incidentId, ICheckInService svc) =>
{
    var checkIns = await svc.GetForIncidentAsync(incidentId);
    return Results.Ok(checkIns);
}).RequireAuthorization("ResponderAccess");

// === Designated Responder Endpoints ===

app.MapPost("/api/designated-responders", async (SignupDesignatedResponderRequest request, IDesignatedResponderService svc) =>
{
    var responder = await svc.SignupAsync(request);
    return Results.Created($"/api/designated-responders/{responder.Id}", responder);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/designated-responders", async (
    IDesignatedResponderService svc,
    int? page,
    int? pageSize,
    DesignatedResponderStatus? status) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, status);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/designated-responders/{id:guid}", async (Guid id, IDesignatedResponderService svc) =>
{
    var responder = await svc.GetByIdAsync(id);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapPut("/api/designated-responders/{id:guid}/status", async (Guid id, UpdateDesignatedResponderStatusRequest request, IDesignatedResponderService svc) =>
{
    var responder = await svc.UpdateStatusAsync(id, request);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/designated-responders/map", async (IDesignatedResponderService svc, DesignatedResponderStatus? status) =>
{
    var items = await svc.GetMapItemsAsync(status);
    return Results.Ok(items);
}).RequireAuthorization("ResponderAccess");

// SignalR hub endpoints (/hubs/responders, /hubs/checkins)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
