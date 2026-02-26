using Hangfire;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P6.FirstResponder;
using TheWatch.P6.FirstResponder.Responders;
using TheWatch.P6.FirstResponder.Services;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.AddWatchKafka();
builder.AddWatchKafkaConsumer<IncidentCreatedConsumer>();

// CORS (configured for SignalR — requires AllowCredentials)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

// SignalR real-time hubs (ResponderHub, CheckInHub)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<IResponderService, ResponderService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.AddWatchSecurity();

var app = builder.Build();

app.UseCors();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<ICheckInService>(
    "checkin-cleanup",
    svc => svc.CleanupOldCheckInsAsync(TimeSpan.FromHours(72)),
    "0 3 * * *"); // Daily at 3 AM

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

// SignalR hub endpoints (/hubs/responders, /hubs/checkins)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
