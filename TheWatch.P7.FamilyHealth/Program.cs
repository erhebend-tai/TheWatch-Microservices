using Hangfire;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P7.FamilyHealth;
using TheWatch.P7.FamilyHealth.Family;
using TheWatch.P7.FamilyHealth.Services;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();

// CORS (configured for SignalR — requires AllowCredentials)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

// SignalR real-time hubs (CheckInHub, VitalReadingHub, MedicalAlertHub, etc.)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddSingleton<IFamilyService, FamilyService>();
builder.Services.AddSingleton<ICheckInService, CheckInService>();
builder.Services.AddSingleton<IVitalService, VitalService>();

var app = builder.Build();

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseHangfireDashboard("/hangfire");

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<ICheckInService>(
    "checkin-cleanup",
    svc => svc.CleanupOldCheckInsAsync(TimeSpan.FromDays(30)),
    "0 2 * * *"); // Daily at 2 AM

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P7.FamilyHealth",
    "P7",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P7.FamilyHealth",
    Program = "P7",
    Name = "FamilyHealth",
    Description = "Child check-in, vitals, medical alerts",
    Icon = "favorite",
    Version = "0.2.0"
});

// === Family Group Endpoints ===

app.MapPost("/api/families", async (CreateFamilyGroupRequest request, IFamilyService svc) =>
{
    var group = await svc.CreateGroupAsync(request);
    return Results.Created($"/api/families/{group.Id}", group);
});

app.MapGet("/api/families", async (IFamilyService svc) =>
{
    var groups = await svc.ListGroupsAsync();
    return Results.Ok(groups);
});

app.MapGet("/api/families/{id:guid}", async (Guid id, IFamilyService svc) =>
{
    var response = await svc.GetGroupAsync(id);
    return response is not null ? Results.Ok(response) : Results.NotFound();
});

app.MapPost("/api/families/{groupId:guid}/members", async (Guid groupId, AddMemberRequest request, IFamilyService svc) =>
{
    var member = await svc.AddMemberAsync(groupId, request);
    return Results.Created($"/api/members/{member.Id}", member);
});

app.MapGet("/api/members/{id:guid}", async (Guid id, IFamilyService svc) =>
{
    var member = await svc.GetMemberAsync(id);
    return member is not null ? Results.Ok(member) : Results.NotFound();
});

app.MapDelete("/api/members/{memberId:guid}", async (Guid memberId, IFamilyService svc) =>
{
    var ok = await svc.RemoveMemberAsync(memberId);
    return ok ? Results.NoContent() : Results.NotFound();
});

// === Check-In Endpoints ===

app.MapPost("/api/members/{memberId:guid}/checkins", async (Guid memberId, CreateCheckInRequest request, ICheckInService svc, ICheckInBroadcaster checkInHub) =>
{
    var checkIn = await svc.CreateAsync(memberId, request);
    // Broadcast check-in notification to family members watching this member
    await checkInHub.BroadcastCreatedAsync(checkIn, checkIn.MemberId.ToString());
    return Results.Created($"/api/members/{memberId}/checkins/{checkIn.Id}", checkIn);
});

app.MapGet("/api/members/{memberId:guid}/checkins", async (Guid memberId, ICheckInService svc, int? limit) =>
{
    var checkIns = await svc.GetForMemberAsync(memberId, limit ?? 20);
    return Results.Ok(checkIns);
});

// === Vital Endpoints ===

app.MapPost("/api/members/{memberId:guid}/vitals", async (Guid memberId, RecordVitalRequest request, IVitalService svc, IVitalReadingBroadcaster vitalHub) =>
{
    var reading = await svc.RecordAsync(memberId, request);
    // Broadcast vital reading in real-time to family dashboard
    await vitalHub.BroadcastCreatedAsync(reading, reading.MemberId.ToString());
    return Results.Created($"/api/members/{memberId}/vitals/{reading.Id}", reading);
});

app.MapGet("/api/members/{memberId:guid}/vitals", async (Guid memberId, IVitalService svc, VitalType? type, int? limit) =>
{
    var readings = await svc.GetHistoryAsync(memberId, type, limit ?? 50);
    return Results.Ok(readings);
});

app.MapGet("/api/members/{memberId:guid}/alerts", async (Guid memberId, IVitalService svc, bool? unacknowledgedOnly) =>
{
    var alerts = await svc.GetAlertsAsync(memberId, unacknowledgedOnly ?? false);
    return Results.Ok(alerts);
});

app.MapPut("/api/alerts/{alertId:guid}/acknowledge", async (Guid alertId, IVitalService svc, IMedicalAlertBroadcaster alertHub) =>
{
    var alert = await svc.AcknowledgeAlertAsync(alertId);
    if (alert is not null)
    {
        // Broadcast alert acknowledgement to family members
        await alertHub.BroadcastUpdatedAsync(alert, alert.MemberId.ToString());
    }
    return alert is not null ? Results.Ok(alert) : Results.NotFound();
});

// SignalR hub endpoints (/hubs/checkins, /hubs/vitalreadings, /hubs/medicalalerts, etc.)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
