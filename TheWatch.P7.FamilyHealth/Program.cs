using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P7.FamilyHealth;
using TheWatch.P7.FamilyHealth.Family;
using TheWatch.P7.FamilyHealth.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P7.FamilyHealth.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Wearable;
using TheWatch.Contracts.CoreGateway;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

// CORS (SignalR — requires AllowCredentials)
builder.Services.AddWatchCors(builder.Configuration, requiresSignalR: true);

// SignalR real-time hubs (CheckInHub, VitalReadingHub, MedicalAlertHub, etc.)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IVitalService, VitalService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, FamilyHealthSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// IWearableClient — query device heartbeat/vital data for health monitoring
builder.Services.AddWearableClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:Wearable"] ?? "https+http://p4-wearable");

// ICoreGatewayClient — user profile lookups for family member resolution
builder.Services.AddCoreGatewayClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:CoreGateway"] ?? "https+http://p1-coregateway");

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
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/families", async (IFamilyService svc) =>
{
    var groups = await svc.ListGroupsAsync();
    return Results.Ok(groups);
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/families/{id:guid}", async (Guid id, IFamilyService svc) =>
{
    var response = await svc.GetGroupAsync(id);
    return response is not null ? Results.Ok(response) : Results.NotFound();
}).RequireAuthorization("FamilyAccess");

app.MapPost("/api/families/{groupId:guid}/members", async (Guid groupId, AddMemberRequest request, IFamilyService svc) =>
{
    var member = await svc.AddMemberAsync(groupId, request);
    return Results.Created($"/api/members/{member.Id}", member);
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/members/{id:guid}", async (Guid id, IFamilyService svc) =>
{
    var member = await svc.GetMemberAsync(id);
    return member is not null ? Results.Ok(member) : Results.NotFound();
}).RequireAuthorization("FamilyAccess");

app.MapDelete("/api/members/{memberId:guid}", async (Guid memberId, IFamilyService svc) =>
{
    var ok = await svc.RemoveMemberAsync(memberId);
    return ok ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("FamilyAccess");

// === Check-In Endpoints ===

app.MapPost("/api/members/{memberId:guid}/checkins", async (Guid memberId, CreateCheckInRequest request, ICheckInService svc, ICheckInBroadcaster checkInHub) =>
{
    var checkIn = await svc.CreateAsync(memberId, request);
    // Broadcast check-in notification to family members watching this member
    await checkInHub.BroadcastCreatedAsync(checkIn, checkIn.MemberId.ToString());
    return Results.Created($"/api/members/{memberId}/checkins/{checkIn.Id}", checkIn);
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/members/{memberId:guid}/checkins", async (Guid memberId, ICheckInService svc, int? limit) =>
{
    var checkIns = await svc.GetForMemberAsync(memberId, limit ?? 20);
    return Results.Ok(checkIns);
}).RequireAuthorization("FamilyAccess");

// === Vital Endpoints ===

app.MapPost("/api/members/{memberId:guid}/vitals", async (Guid memberId, RecordVitalRequest request, IVitalService svc, IVitalReadingBroadcaster vitalHub) =>
{
    var reading = await svc.RecordAsync(memberId, request);
    // Broadcast vital reading in real-time to family dashboard
    await vitalHub.BroadcastCreatedAsync(reading, reading.MemberId.ToString());
    return Results.Created($"/api/members/{memberId}/vitals/{reading.Id}", reading);
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/members/{memberId:guid}/vitals", async (Guid memberId, IVitalService svc, VitalType? type, int? limit) =>
{
    var readings = await svc.GetHistoryAsync(memberId, type, limit ?? 50);
    return Results.Ok(readings);
}).RequireAuthorization("FamilyAccess");

app.MapGet("/api/members/{memberId:guid}/alerts", async (Guid memberId, IVitalService svc, bool? unacknowledgedOnly) =>
{
    var alerts = await svc.GetAlertsAsync(memberId, unacknowledgedOnly ?? false);
    return Results.Ok(alerts);
}).RequireAuthorization("FamilyAccess");

app.MapPut("/api/alerts/{alertId:guid}/acknowledge", async (Guid alertId, IVitalService svc, IMedicalAlertBroadcaster alertHub) =>
{
    var alert = await svc.AcknowledgeAlertAsync(alertId);
    if (alert is not null)
    {
        // Broadcast alert acknowledgement to family members
        await alertHub.BroadcastUpdatedAsync(alert, alert.MemberId.ToString());
    }
    return alert is not null ? Results.Ok(alert) : Results.NotFound();
}).RequireAuthorization("FamilyAccess");

// SignalR hub endpoints (/hubs/checkins, /hubs/vitalreadings, /hubs/medicalalerts, etc.)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
