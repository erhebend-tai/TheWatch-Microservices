using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P10.Gamification;
using TheWatch.P10.Gamification.Gaming;
using TheWatch.P10.Gamification.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P10.Gamification.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.CoreGateway;
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
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

builder.Services.AddWatchCors(builder.Configuration);

builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, GamificationSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// ICoreGatewayClient — user profile lookups for player/badge award
builder.Services.AddCoreGatewayClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:CoreGateway"] ?? "https+http://p1-coregateway");

// IVoiceEmergencyClient — award points on incident reporting and resolution
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
builder.Services.AddWatchTracing("TheWatch.P10.Gamification");
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
RecurringJob.AddOrUpdate<IGamificationService>(
    "expire-challenges",
    svc => svc.ExpireChallengesAsync(),
    "0 * * * *"); // Every hour

// Item 246: Readiness probe — checks SQL Server, Redis, Kafka, PostGIS connectivity
app.MapHealthChecks("/health/ready");
// Item 249: Canary endpoints for synthetic monitoring (/canary + /canary/deep)
app.MapWatchCanaryEndpoints("TheWatch.P10.Gamification");

app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P10.Gamification", "P10", "Healthy", DateTime.UtcNow));

app.MapGet("/info", () => new
{
    Service = "TheWatch.P10.Gamification",
    Program = "P10",
    Name = "Gamification",
    Description = "Rewards, challenges, leaderboards",
    Icon = "emoji_events",
    Version = "0.2.0"
});

// === Reward Endpoints ===

app.MapGet("/api/rewards/{userId:guid}", async (Guid userId, IGamificationService svc) =>
{
    var reward = await svc.GetOrCreateRewardAsync(userId);
    return Results.Ok(reward);
}).RequireAuthorization("Authenticated");

app.MapPost("/api/rewards/points", async (AwardPointsRequest request, IGamificationService svc) =>
{
    var reward = await svc.AwardPointsAsync(request);
    return Results.Ok(reward);
}).RequireAuthorization("Authenticated");

app.MapPost("/api/rewards/badges", async (AwardBadgeRequest request, IGamificationService svc) =>
{
    var reward = await svc.AwardBadgeAsync(request);
    return Results.Ok(reward);
}).RequireAuthorization("Authenticated");

// === Challenge Endpoints ===

app.MapPost("/api/challenges", async (CreateChallengeRequest request, IGamificationService svc) =>
{
    var challenge = await svc.CreateChallengeAsync(request);
    return Results.Created($"/api/challenges/{challenge.Id}", challenge);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/challenges", async (IGamificationService svc, ChallengeStatus? status) =>
{
    var challenges = await svc.ListChallengesAsync(status);
    return Results.Ok(challenges);
}).RequireAuthorization("Authenticated");

// === Leaderboard ===

app.MapGet("/api/leaderboard", async (IGamificationService svc, int? top) =>
{
    var leaderboard = await svc.GetLeaderboardAsync(top ?? 50);
    return Results.Ok(leaderboard);
}).RequireAuthorization("Authenticated");

app.Run();

public partial class Program { }
