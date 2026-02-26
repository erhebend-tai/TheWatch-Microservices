using Hangfire;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P1.CoreGateway;
using TheWatch.P1.CoreGateway.Core;
using TheWatch.P1.CoreGateway.Services;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<IProfileService, ProfileService>();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddHttpClient("services");

var app = builder.Build();

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseHangfireDashboard("/hangfire");

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
});

app.MapGet("/api/profiles", async (IProfileService svc, int? page, int? pageSize, UserRole? role) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20, role);
    return Results.Ok(result);
});

app.MapGet("/api/profiles/{id:guid}", async (Guid id, IProfileService svc) =>
{
    var profile = await svc.GetByIdAsync(id);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
});

app.MapPut("/api/profiles/{id:guid}", async (Guid id, UpdateProfileRequest request, IProfileService svc) =>
{
    var profile = await svc.UpdateAsync(id, request);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
});

app.MapPut("/api/profiles/{id:guid}/preferences", async (Guid id, SetPreferenceRequest request, IProfileService svc) =>
{
    var profile = await svc.SetPreferenceAsync(id, request);
    return profile is not null ? Results.Ok(profile) : Results.NotFound();
});

app.MapDelete("/api/profiles/{id:guid}", async (Guid id, IProfileService svc) =>
{
    var ok = await svc.DeactivateAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

// === Config Endpoints ===

app.MapPost("/api/config", async (SetConfigRequest request, IConfigService svc) =>
{
    var config = await svc.SetAsync(request);
    return Results.Ok(config);
});

app.MapGet("/api/config/{key}", async (string key, IConfigService svc) =>
{
    var config = await svc.GetAsync(key);
    return config is not null ? Results.Ok(config) : Results.NotFound();
});

app.MapGet("/api/config", async (IConfigService svc) =>
{
    var configs = await svc.ListAllAsync();
    return Results.Ok(configs);
});

// === Service Health Aggregation ===

app.MapGet("/api/services/health", async (IConfigService svc, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("services");
    var summary = await svc.CheckAllServicesAsync(client);
    return Results.Ok(summary);
});

app.Run();

public partial class Program { }
