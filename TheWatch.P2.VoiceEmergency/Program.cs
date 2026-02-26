using Hangfire;
using Hangfire.InMemory;
using Hangfire.Batches;
using Serilog;
using TheWatch.P2.VoiceEmergency;
using TheWatch.P2.VoiceEmergency.Emergency;
using TheWatch.P2.VoiceEmergency.Services;
using TheWatch.Shared.Contracts;
using TheWatch.Shared.Events;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.AddWatchKafka();
builder.ConfigureWatchNotifications();

// CORS (configured for SignalR — requires AllowCredentials)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

// SignalR real-time hubs (IncidentHub, DispatchHub)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddSingleton<IEmergencyService, EmergencyService>();
builder.Services.AddSingleton<IDispatchService, DispatchService>();

var app = builder.Build();

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseHangfireDashboard("/hangfire");

// Register recurring Hangfire jobs
RecurringJob.AddOrUpdate<IDispatchService>(
    "dispatch-escalation",
    svc => svc.EscalateUnacknowledgedAsync(TimeSpan.FromMinutes(5)),
    "*/2 * * * *"); // Every 2 minutes

RecurringJob.AddOrUpdate<IEmergencyService>(
    "incident-cleanup",
    svc => svc.ArchiveResolvedAsync(TimeSpan.FromHours(24)),
    "0 * * * *"); // Every hour

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P2.VoiceEmergency",
    "P2",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P2.VoiceEmergency",
    Program = "P2",
    Name = "VoiceEmergency",
    Description = "Emergency reporting, dispatch, voice SOS",
    Icon = "emergency",
    Version = "0.2.0"
});

// === Incident Endpoints ===

app.MapPost("/api/incidents", async (CreateIncidentRequest request, IEmergencyService svc, IEventPublisher events, IIncidentBroadcaster hub) =>
{
    var incident = await svc.CreateIncidentAsync(request);

    // Broadcast to SignalR clients in real-time
    await hub.BroadcastCreatedAsync(incident);

    // Publish IncidentCreated event to Kafka for P6/P3 consumption
    await events.PublishAsync(WatchTopics.IncidentCreated, incident.Id.ToString(), new IncidentCreatedEvent
    {
        SourceService = "TheWatch.P2.VoiceEmergency",
        IncidentId = incident.Id,
        EmergencyType = incident.Type.ToString(),
        Description = incident.Description,
        Latitude = incident.Location.Latitude,
        Longitude = incident.Location.Longitude,
        ReporterId = incident.ReporterId,
        Severity = incident.Severity
    });

    return Results.Created($"/api/incidents/{incident.Id}", incident);
});

app.MapGet("/api/incidents", async (
    IEmergencyService svc,
    int? page,
    int? pageSize,
    IncidentStatus? status,
    EmergencyType? type) =>
{
    var result = await svc.ListIncidentsAsync(page ?? 1, pageSize ?? 20, status, type);
    return Results.Ok(result);
});

app.MapGet("/api/incidents/{id:guid}", async (Guid id, IEmergencyService svc) =>
{
    var incident = await svc.GetIncidentAsync(id);
    return incident is not null ? Results.Ok(incident) : Results.NotFound();
});

app.MapPut("/api/incidents/{id:guid}/status", async (Guid id, UpdateIncidentStatusRequest request, IEmergencyService svc, IIncidentBroadcaster hub) =>
{
    var incident = await svc.UpdateStatusAsync(id, request);
    if (incident is not null)
    {
        // Broadcast status change to SignalR clients watching this incident
        await hub.BroadcastUpdatedAsync(incident, incident.Id.ToString());
    }
    return incident is not null ? Results.Ok(incident) : Results.NotFound();
});

// === Dispatch Endpoints ===

app.MapPost("/api/dispatch", async (CreateDispatchRequest request, IDispatchService svc, IEmergencyService emergencySvc, IEventPublisher events, IDispatchBroadcaster dispatchHub) =>
{
    var dispatch = await svc.CreateDispatchAsync(request);

    // Broadcast dispatch creation to SignalR clients
    await dispatchHub.BroadcastCreatedAsync(dispatch, dispatch.IncidentId.ToString());

    // Look up incident for location data
    var incident = await emergencySvc.GetIncidentAsync(request.IncidentId);

    // Publish DispatchRequested event to Kafka for P3 mesh broadcast
    await events.PublishAsync(WatchTopics.DispatchRequested, dispatch.IncidentId.ToString(), new DispatchRequestedEvent
    {
        SourceService = "TheWatch.P2.VoiceEmergency",
        DispatchId = dispatch.Id,
        IncidentId = dispatch.IncidentId,
        RadiusKm = dispatch.RadiusKm,
        RespondersRequested = dispatch.RespondersRequested,
        Latitude = incident?.Location.Latitude ?? 0,
        Longitude = incident?.Location.Longitude ?? 0
    });

    return Results.Created($"/api/dispatch/{dispatch.Id}", dispatch);
});

app.MapGet("/api/dispatch/{id:guid}", async (Guid id, IDispatchService svc) =>
{
    var dispatch = await svc.GetDispatchAsync(id);
    return dispatch is not null ? Results.Ok(dispatch) : Results.NotFound();
});

app.MapPost("/api/dispatch/{id:guid}/expand", async (Guid id, ExpandRadiusRequest request, IDispatchService svc) =>
{
    var dispatch = await svc.ExpandRadiusAsync(id, request);
    return dispatch is not null ? Results.Ok(dispatch) : Results.NotFound();
});

app.MapGet("/api/incidents/{incidentId:guid}/dispatches", async (Guid incidentId, IDispatchService svc) =>
{
    var dispatches = await svc.GetDispatchesForIncidentAsync(incidentId);
    return Results.Ok(dispatches);
});

// SignalR hub endpoints (/hubs/incidents, /hubs/dispatches)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
