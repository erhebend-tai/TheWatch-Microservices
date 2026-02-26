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

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Hangfire with InMemory storage
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddSingleton<IResponderService, ResponderService>();
builder.Services.AddSingleton<ICheckInService, CheckInService>();

var app = builder.Build();

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
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
});

app.MapGet("/api/responders", async (
    IResponderService svc,
    int? page,
    int? pageSize,
    ResponderType? type,
    ResponderStatus? status) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, type, status);
    return Results.Ok(result);
});

app.MapGet("/api/responders/{id:guid}", async (Guid id, IResponderService svc) =>
{
    var responder = await svc.GetByIdAsync(id);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
});

app.MapPut("/api/responders/{id:guid}/location", async (Guid id, UpdateLocationRequest request, IResponderService svc) =>
{
    var responder = await svc.UpdateLocationAsync(id, request);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
});

app.MapPut("/api/responders/{id:guid}/status", async (Guid id, UpdateStatusRequest request, IResponderService svc) =>
{
    var responder = await svc.UpdateStatusAsync(id, request);
    return responder is not null ? Results.Ok(responder) : Results.NotFound();
});

app.MapDelete("/api/responders/{id:guid}", async (Guid id, IResponderService svc) =>
{
    var ok = await svc.DeactivateAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

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
});

// === Check-In Endpoints ===

app.MapPost("/api/responders/{responderId:guid}/checkins", async (Guid responderId, CreateCheckInRequest request, ICheckInService svc) =>
{
    var checkIn = await svc.CreateAsync(responderId, request);
    return Results.Created($"/api/responders/{responderId}/checkins/{checkIn.Id}", checkIn);
});

app.MapGet("/api/responders/{responderId:guid}/checkins", async (Guid responderId, ICheckInService svc) =>
{
    var checkIns = await svc.GetForResponderAsync(responderId);
    return Results.Ok(checkIns);
});

app.MapGet("/api/incidents/{incidentId:guid}/checkins", async (Guid incidentId, ICheckInService svc) =>
{
    var checkIns = await svc.GetForIncidentAsync(incidentId);
    return Results.Ok(checkIns);
});

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
