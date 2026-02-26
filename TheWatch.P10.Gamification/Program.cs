using Hangfire;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P10.Gamification;
using TheWatch.P10.Gamification.Gaming;
using TheWatch.P10.Gamification.Services;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.AddWatchSecurity();

var app = builder.Build();

app.UseCors();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<IGamificationService>(
    "expire-challenges",
    svc => svc.ExpireChallengesAsync(),
    "0 * * * *"); // Every hour

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
