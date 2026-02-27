using Hangfire;
using Hangfire.InMemory;
using Hangfire.Batches;
using Serilog;
using TheWatch.P2.VoiceEmergency;
using TheWatch.P2.VoiceEmergency.Emergency;
using TheWatch.P2.VoiceEmergency.Services;
using TheWatch.Shared.Contracts;
using TheWatch.Shared.Events;
using TheWatch.P2.VoiceEmergency.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.Surveillance;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.AddWatchKafka();
builder.AddWatchKafkaConsumer<TheWatch.P2.VoiceEmergency.Services.FootageSubmittedConsumer>();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

// CORS (SignalR — requires AllowCredentials)
builder.Services.AddWatchCors(builder.Configuration, requiresSignalR: true);

// SignalR real-time hubs (IncidentHub, DispatchHub)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Security (JWT validation + rate limiting + audit from SecurityGenerator)
builder.AddWatchSecurity();

// ── Inter-service typed HTTP clients (Items 209, 210, 215) ──
// Shared delegating handlers for correlation ID + API key auth (Items 218, 219)
builder.Services.AddWatchClientHandlers();

// Item 209: ICoreGatewayClient — user profile lookups during incident creation
builder.Services.AddCoreGatewayClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:CoreGateway"] ?? "https+http://p1-coregateway");

// Item 210: IFirstResponderClient — responder dispatch coordination (find nearby, update status)
builder.Services.AddFirstResponderClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:FirstResponder"] ?? "https+http://p6-firstresponder");

// Item 215: ISurveillanceClient — nearby camera/footage lookup during active incidents
builder.Services.AddSurveillanceClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:Surveillance"] ?? "https+http://p11-surveillance");

// Services
builder.Services.AddScoped<IEmergencyService, EmergencyService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<IEmergencyCallService, EmergencyCallService>();
builder.Services.AddScoped<ITriageService, TriageService>();
builder.Services.AddScoped<IWatchDataSeeder, VoiceEmergencySeeder>();
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
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/incidents/{id:guid}", async (Guid id, IEmergencyService svc) =>
{
    var incident = await svc.GetIncidentAsync(id);
    return incident is not null ? Results.Ok(incident) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPut("/api/incidents/{id:guid}/status", async (Guid id, UpdateIncidentStatusRequest request, IEmergencyService svc, IIncidentBroadcaster hub) =>
{
    var incident = await svc.UpdateStatusAsync(id, request);
    if (incident is not null)
    {
        // Broadcast status change to SignalR clients watching this incident
        await hub.BroadcastUpdatedAsync(incident, incident.Id.ToString());
    }
    return incident is not null ? Results.Ok(incident) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

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
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/dispatch/{id:guid}", async (Guid id, IDispatchService svc) =>
{
    var dispatch = await svc.GetDispatchAsync(id);
    return dispatch is not null ? Results.Ok(dispatch) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapPost("/api/dispatch/{id:guid}/expand", async (Guid id, ExpandRadiusRequest request, IDispatchService svc) =>
{
    var dispatch = await svc.ExpandRadiusAsync(id, request);
    return dispatch is not null ? Results.Ok(dispatch) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/incidents/{incidentId:guid}/dispatches", async (Guid incidentId, IDispatchService svc) =>
{
    var dispatches = await svc.GetDispatchesForIncidentAsync(incidentId);
    return Results.Ok(dispatches);
}).RequireAuthorization("ResponderAccess");

// SignalR hub endpoints (/hubs/incidents, /hubs/dispatches)
app.MapWatchHubs();

// === Triage Intake Endpoints ===

app.MapPost("/api/triage", async (LogTriageIntakeRequest request, ITriageService svc) =>
{
    var intake = await svc.LogIntakeAsync(request);
    return Results.Created($"/api/triage/{intake.Id}", intake);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/triage/{id:guid}", async (Guid id, ITriageService svc) =>
{
    var intake = await svc.GetIntakeAsync(id);
    return intake is not null ? Results.Ok(intake) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapGet("/api/incidents/{incidentId:guid}/triage", async (Guid incidentId, ITriageService svc) =>
{
    var intakes = await svc.GetIntakesForIncidentAsync(incidentId);
    return Results.Ok(intakes);
}).RequireAuthorization("ResponderAccess");

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
