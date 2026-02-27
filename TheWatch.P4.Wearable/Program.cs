using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P4.Wearable;
using TheWatch.P4.Wearable.Devices;
using TheWatch.P4.Wearable.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P4.Wearable.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.FamilyHealth;
using TheWatch.Contracts.CoreGateway;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

builder.Services.AddWatchCors(builder.Configuration);

builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, WearableSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// IFamilyHealthClient — forward vitals/alerts to family health service
builder.Services.AddFamilyHealthClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:FamilyHealth"] ?? "https+http://p7-familyhealth");

// ICoreGatewayClient — user profile lookups for device owner resolution
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
RecurringJob.AddOrUpdate<IDeviceService>(
    "stale-device-detection",
    svc => svc.MarkStaleDevicesOfflineAsync(TimeSpan.FromHours(1)),
    "*/10 * * * *"); // Every 10 minutes

RecurringJob.AddOrUpdate<IDeviceService>(
    "heartbeat-data-cleanup",
    svc => svc.CleanupOldHeartbeatReadingsAsync(TimeSpan.FromDays(90)),
    "0 4 * * *"); // Daily at 4 AM

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P4.Wearable", "P4", "Healthy", DateTime.UtcNow));

app.MapGet("/info", () => new
{
    Service = "TheWatch.P4.Wearable",
    Program = "P4",
    Name = "Wearable",
    Description = "Device sync, heartbeat, offline queue",
    Icon = "watch",
    Version = "0.2.0"
});

// === Device Endpoints ===

app.MapPost("/api/devices", async (RegisterDeviceRequest request, IDeviceService svc) =>
{
    var device = await svc.RegisterAsync(request);
    return Results.Created($"/api/devices/{device.Id}", device);
});

app.MapGet("/api/devices", async (IDeviceService svc, Guid? ownerId, DevicePlatform? platform) =>
{
    var result = await svc.ListAsync(ownerId, platform);
    return Results.Ok(result);
});

app.MapGet("/api/devices/{id:guid}", async (Guid id, IDeviceService svc) =>
{
    var device = await svc.GetByIdAsync(id);
    return device is not null ? Results.Ok(device) : Results.NotFound();
});

app.MapPut("/api/devices/{id:guid}/status", async (Guid id, UpdateDeviceStatusRequest request, IDeviceService svc) =>
{
    var device = await svc.UpdateStatusAsync(id, request);
    return device is not null ? Results.Ok(device) : Results.NotFound();
});

// === Heartbeat Endpoints ===

app.MapPost("/api/devices/{deviceId:guid}/heartbeats", async (Guid deviceId, RecordHeartbeatRequest request, IDeviceService svc) =>
{
    var reading = await svc.RecordHeartbeatAsync(deviceId, request);
    return Results.Created($"/api/devices/{deviceId}/heartbeats/{reading.Id}", reading);
});

app.MapGet("/api/devices/{deviceId:guid}/heartbeats", async (Guid deviceId, IDeviceService svc, int? limit) =>
{
    var history = await svc.GetHeartbeatHistoryAsync(deviceId, limit ?? 100);
    return Results.Ok(history);
});

// === Sync Endpoints ===

app.MapPost("/api/devices/{deviceId:guid}/sync", async (Guid deviceId, StartSyncRequest request, IDeviceService svc) =>
{
    var job = await svc.StartSyncAsync(deviceId, request);
    return Results.Created($"/api/devices/{deviceId}/sync/{job.Id}", job);
});

app.MapGet("/api/devices/{deviceId:guid}/sync", async (Guid deviceId, IDeviceService svc, int? limit) =>
{
    var history = await svc.GetSyncHistoryAsync(deviceId, limit ?? 20);
    return Results.Ok(history);
});

app.Run();

public partial class Program { }
