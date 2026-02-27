using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P1.CoreGateway;
using TheWatch.P1.CoreGateway.Core;
using TheWatch.P1.CoreGateway.Services;
using TheWatch.Shared.Contracts;
using TheWatch.Shared.Notifications;
using TheWatch.P1.CoreGateway.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;

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

builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddHttpClient("services");
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, CoreGatewaySeeder>();
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
RecurringJob.AddOrUpdate<IConfigService>(
    "service-health-check",
    svc => svc.RunScheduledHealthCheckAsync(),
    "*/5 * * * *"); // Every 5 minutes

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P1.CoreGateway", "P1", "Healthy", DateTime.UtcNow));

app.MapGet("/info", () => new
{
    Service = "TheWatch.P1.CoreGateway",
    Program = "P1",
    Name = "CoreGateway",
    Description = "API Gateway, user profiles, platform config",
    Icon = "hub",
    Version = "0.2.0"
});

// === Profile Endpoints ===

app.MapPost("/api/profiles", async (CreateProfileRequest request, IProfileService svc) =>
{
    var profile = await svc.CreateAsync(request);
    return Results.Created($"/api/profiles/{profile.Id}", profile);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/profiles", async (IProfileService svc, int? page, int? pageSize, UserRole? role) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, role);
    return Results.Ok(result);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/profiles/{id:guid}", async (Guid id, IProfileService svc) =>
{
    var profile = await svc.GetByIdAsync(id);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPut("/api/profiles/{id:guid}", async (Guid id, UpdateProfileRequest request, IProfileService svc) =>
{
    var profile = await svc.UpdateAsync(id, request);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPut("/api/profiles/{id:guid}/preferences", async (Guid id, SetPreferenceRequest request, IProfileService svc) =>
{
    var profile = await svc.SetPreferenceAsync(id, request);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapDelete("/api/profiles/{id:guid}", async (Guid id, IProfileService svc) =>
{
    var ok = await svc.DeactivateAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("Authenticated");

// === Config Endpoints ===

app.MapPost("/api/config", async (SetConfigRequest request, IConfigService svc) =>
{
    var config = await svc.SetAsync(request);
    return Results.Ok(config);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/config/{key}", async (string key, IConfigService svc) =>
{
    var config = await svc.GetAsync(key);
    return config is not null ? Results.Ok(config) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/config", async (IConfigService svc) =>
{
    var configs = await svc.ListAllAsync();
    return Results.Ok(configs);
}).RequireAuthorization("AdminOnly");

// === Service Health Aggregation ===

app.MapGet("/api/services/health", async (IConfigService svc, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("services");
    var summary = await svc.CheckAllServicesAsync(client);
    return Results.Ok(summary);
});

// === Device Registration Endpoints (Push Notifications) ===

var deviceRegistrations = new System.Collections.Concurrent.ConcurrentDictionary<Guid, DeviceRegistration>();

app.MapPost("/api/devices/register", async (DeviceRegistration registration, INotificationService notifications) =>
{
    // Deactivate any existing registration for this user + platform
    foreach (var existing in deviceRegistrations.Values
        .Where(d => d.UserId == registration.UserId && d.Platform == registration.Platform && d.IsActive))
    {
        existing.IsActive = false;
    }

    registration.RegisteredAt = DateTime.UtcNow;
    registration.LastActiveAt = DateTime.UtcNow;
    registration.IsActive = true;
    deviceRegistrations[registration.Id] = registration;

    // Subscribe to default topics
    foreach (var topic in registration.SubscribedTopics)
    {
        await notifications.SubscribeToTopicAsync(registration.DeviceToken, topic);
    }

    return Results.Created($"/api/devices/{registration.Id}", registration);
});

app.MapGet("/api/devices/user/{userId:guid}", (Guid userId) =>
{
    var devices = deviceRegistrations.Values
        .Where(d => d.UserId == userId && d.IsActive)
        .ToList();
    return Results.Ok(devices);
});

app.MapDelete("/api/devices/{id:guid}", (Guid id) =>
{
    if (deviceRegistrations.TryGetValue(id, out var reg))
    {
        reg.IsActive = false;
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.MapPost("/api/devices/{id:guid}/topics/{topic}", async (Guid id, string topic, INotificationService notifications) =>
{
    if (!deviceRegistrations.TryGetValue(id, out var reg) || !reg.IsActive)
        return Results.NotFound();

    await notifications.SubscribeToTopicAsync(reg.DeviceToken, topic);
    if (!reg.SubscribedTopics.Contains(topic))
        reg.SubscribedTopics.Add(topic);

    return Results.Ok(reg);
});

app.MapDelete("/api/devices/{id:guid}/topics/{topic}", async (Guid id, string topic, INotificationService notifications) =>
{
    if (!deviceRegistrations.TryGetValue(id, out var reg) || !reg.IsActive)
        return Results.NotFound();

    await notifications.UnsubscribeFromTopicAsync(reg.DeviceToken, topic);
    reg.SubscribedTopics.Remove(topic);

    return Results.Ok(reg);
});

app.Run();

public partial class Program { }
