using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P3.MeshNetwork;
using TheWatch.P3.MeshNetwork.Mesh;
using TheWatch.P3.MeshNetwork.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P3.MeshNetwork.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.DisasterRelief;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Shared.Health;
using FluentValidation;
using TheWatch.Shared.Api;
using TheWatch.Shared.Observability;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.AddWatchKafka();
builder.AddWatchKafkaConsumer<DispatchRequestedConsumer>();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

builder.Services.AddWatchCors(builder.Configuration);

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<IMeshService, MeshService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, MeshNetworkSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// IDisasterReliefClient — query shelter/resource data for mesh broadcasts
builder.Services.AddDisasterReliefClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:DisasterRelief"] ?? "https+http://p8-disasterrelief");

// IVoiceEmergencyClient — fetch active incidents for mesh alert prioritization
builder.Services.AddVoiceEmergencyClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:VoiceEmergency"] ?? "https+http://p2-voiceemergency");

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
builder.Services.AddWatchTracing("TheWatch.P3.MeshNetwork");
var app = builder.Build();
await app.UseWatchMigrations();

app.UseCors();
app.UseWatchMetrics();
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

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<IMeshService>(
    "stale-node-cleanup",
    svc => svc.CleanupStaleNodesAsync(TimeSpan.FromMinutes(30)),
    "*/10 * * * *"); // Every 10 minutes

// Health endpoint
// Item 246: Readiness probe — checks SQL Server, Redis, Kafka, PostGIS connectivity
app.MapHealthChecks("/health/ready");
// Item 249: Canary endpoints for synthetic monitoring (/canary + /canary/deep)
app.MapWatchCanaryEndpoints("TheWatch.P3.MeshNetwork");

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P3.MeshNetwork",
    "P3",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P3.MeshNetwork",
    Program = "P3",
    Name = "MeshNetwork",
    Description = "Messaging, notifications, mesh relay",
    Icon = "cell_tower",
    Version = "0.2.0"
});

// === Node Endpoints ===

app.MapPost("/api/nodes", async (RegisterNodeRequest request, IMeshService svc) =>
{
    var node = await svc.RegisterNodeAsync(request);
    return Results.Created($"/api/nodes/{node.Id}", node);
});

app.MapGet("/api/nodes", async (IMeshService svc, NodeStatus? status) =>
{
    var result = await svc.ListNodesAsync(status);
    return Results.Ok(result);
});

app.MapGet("/api/nodes/{id:guid}", async (Guid id, IMeshService svc) =>
{
    var node = await svc.GetNodeAsync(id);
    return node is not null ? Results.Ok(node) : Results.NotFound();
});

app.MapPut("/api/nodes/{id:guid}/status", async (Guid id, UpdateNodeStatusRequest request, IMeshService svc) =>
{
    var node = await svc.UpdateNodeStatusAsync(id, request);
    return node is not null ? Results.Ok(node) : Results.NotFound();
});

app.MapGet("/api/topology", async (IMeshService svc) =>
{
    var topology = await svc.GetTopologyAsync();
    return Results.Ok(topology);
});

// === Message Endpoints ===

app.MapPost("/api/messages", async (SendMessageRequest request, INotificationService svc) =>
{
    var msg = await svc.SendAsync(request);
    return Results.Created($"/api/messages/{msg.Id}", msg);
});

app.MapGet("/api/nodes/{nodeId:guid}/messages", async (Guid nodeId, INotificationService svc, int? limit) =>
{
    var msgs = await svc.GetMessagesAsync(nodeId, limit ?? 50);
    return Results.Ok(msgs);
});

// === Channel Endpoints ===

app.MapPost("/api/channels", async (CreateChannelRequest request, INotificationService svc) =>
{
    var channel = await svc.CreateChannelAsync(request);
    return Results.Created($"/api/channels/{channel.Id}", channel);
});

app.MapGet("/api/channels", async (INotificationService svc) =>
{
    var channels = await svc.ListChannelsAsync();
    return Results.Ok(channels);
});

app.MapPost("/api/channels/{channelId:guid}/subscribe/{nodeId:guid}", async (Guid channelId, Guid nodeId, INotificationService svc) =>
{
    var ok = await svc.SubscribeAsync(channelId, nodeId);
    return ok ? Results.Ok() : Results.NotFound();
});

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
