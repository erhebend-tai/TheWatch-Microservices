using Hangfire;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P12.Notifications;
using TheWatch.P12.Notifications.Notifications;
using TheWatch.P12.Notifications.Services;
using TheWatch.P12.Notifications.Events;
using TheWatch.Shared.Contracts;
using TheWatch.P12.Notifications.Data.Seeders;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Shared.Health;
using FluentValidation;
using TheWatch.Shared.Api;
using TheWatch.Shared.Observability;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.AddWatchKafka();
builder.AddWatchKafkaConsumer<IncidentCreatedNotificationConsumer>();

// CORS
builder.Services.AddWatchCors(builder.Configuration);

// Hangfire with InMemory storage
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Domain Services
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IPreferenceService, PreferenceService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, NotificationsSeeder>();
builder.Services.AddWatchResponseCompression();

// Contract client wiring
builder.Services.AddWatchClientHandlers();

// ICoreGatewayClient — user profile lookups for notification targeting
builder.Services.AddCoreGatewayClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:CoreGateway"] ?? "https+http://p1-coregateway");

builder.AddWatchControllers();

// Item 246: Dependency health checks (SQL Server, Redis, Kafka, PostGIS connectivity)
builder.Services.AddWatchHealthChecks(builder.Configuration);
// Item 226: Register FluentValidation validators for all request DTOs [STIG V-222606, OWASP A03]
builder.Services.AddValidatorsFromAssemblyContaining<Program>(lifetime: ServiceLifetime.Scoped);
// Item 229: API versioning — v1 prefix for current endpoints, header-based negotiation
builder.Services.AddWatchApiVersioning();
// Item 244: Prometheus metrics (request duration, active incidents, SOS, auth failures)
builder.Services.AddWatchMetrics();
// Item 247: Distributed tracing span enrichment (user ID, incident ID, device ID)
builder.Services.AddWatchTracing("TheWatch.P12.Notifications");
var app = builder.Build();
await app.UseWatchMigrations();

// RFC 9457 global exception handler must be first in the pipeline
app.UseWatchExceptionHandler();

app.UseCors();
app.UseWatchMetrics();
app.UseWatchResponseCompression();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
// Item 231: ETag / If-None-Match conditional response support
app.UseWatchETagSupport();
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new TheWatch.Shared.Security.HangfireDashboardAuthFilter()],
    IsReadOnlyFunc = _ => true
});
app.MapWatchControllers();

// Health endpoint
// Item 246: Readiness probe — checks SQL Server, Redis, Kafka, PostGIS connectivity
app.MapHealthChecks("/health/ready");
// Item 249: Canary endpoints for synthetic monitoring (/canary + /canary/deep)
app.MapWatchCanaryEndpoints("TheWatch.P12.Notifications");

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P12.Notifications",
    "P12",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P12.Notifications",
    Program = "P12",
    Name = "Notifications",
    Description = "Push notifications, SMS, email, broadcast alerts, and notification preferences",
    Icon = "notifications",
    Version = "0.1.0"
});

// === Notification Endpoints ===

app.MapPost("/api/notifications/send", async (SendNotificationRequest request, INotificationDispatcher svc) =>
{
    var notification = await svc.SendAsync(request);
    return Results.Created($"/api/notifications/{notification.Id}", notification);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/notifications/{recipientId:guid}", async (
    Guid recipientId,
    INotificationDispatcher svc,
    int? page,
    int? pageSize) =>
{
    var result = await svc.ListAsync(recipientId, page ?? 1, pageSize ?? 20);
    return Results.Ok(result);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/notifications/record/{id:guid}", async (Guid id, INotificationDispatcher svc) =>
{
    var notification = await svc.GetAsync(id);
    return notification is not null ? Results.Ok(notification) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPost("/api/notifications/broadcast", async (BroadcastRequest request, INotificationDispatcher svc) =>
{
    var broadcast = await svc.BroadcastAsync(request);
    return Results.Created($"/api/notifications/broadcasts/{broadcast.Id}", broadcast);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/notifications/stats", async (INotificationDispatcher svc) =>
{
    var stats = await svc.GetStatsAsync();
    return Results.Ok(stats);
}).RequireAuthorization("AdminOnly");

// === Preference Endpoints ===

app.MapPost("/api/notifications/preferences", async (SetNotificationPreferenceRequest request, IPreferenceService svc) =>
{
    var pref = await svc.SetAsync(request);
    return Results.Ok(pref);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/notifications/preferences/{userId:guid}", async (Guid userId, IPreferenceService svc) =>
{
    var prefs = await svc.GetForUserAsync(userId);
    return Results.Ok(prefs);
}).RequireAuthorization("Authenticated");

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
