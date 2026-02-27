using System.Security.Claims;
using System.Text.Json;
using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TheWatch.P5.AuthSecurity;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Middleware;
using TheWatch.P5.AuthSecurity.Models;
using TheWatch.P5.AuthSecurity.Security;
using TheWatch.P5.AuthSecurity.Services;
using TheWatch.Shared.Auth;
using TheWatch.Shared.Contracts;
using HealthResponse = TheWatch.Shared.Contracts.HealthResponse;
using LoginRequest = TheWatch.P5.AuthSecurity.Auth.LoginRequest;
using RegisterRequest = TheWatch.P5.AuthSecurity.Auth.RegisterRequest;
using RefreshTokenRequest = TheWatch.P5.AuthSecurity.Auth.RefreshTokenRequest;
using AssignRoleRequest = TheWatch.P5.AuthSecurity.Auth.AssignRoleRequest;
using MfaVerifyRequest = TheWatch.P5.AuthSecurity.Auth.MfaVerifyRequest;
using SmsMfaSendRequest = TheWatch.P5.AuthSecurity.Auth.SmsMfaSendRequest;
using SmsMfaVerifyRequest = TheWatch.P5.AuthSecurity.Auth.SmsMfaVerifyRequest;
using MagicLinkRequest = TheWatch.P5.AuthSecurity.Auth.MagicLinkRequest;
using ChangePasswordRequest = TheWatch.P5.AuthSecurity.Auth.ChangePasswordRequest;
using CompleteOnboardingStepRequest = TheWatch.P5.AuthSecurity.Auth.CompleteOnboardingStepRequest;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Shared.Health;
using FluentValidation;
using TheWatch.Shared.Api;
using TheWatch.Shared.Observability;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

// CORS (SignalR compatible)
builder.Services.AddWatchCors(builder.Configuration, requiresSignalR: true);

// === Identity + EF Core ===
var connectionString = builder.Configuration.GetConnectionString("authsecuritydb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AuthIdentityDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Fallback for dev/test: InMemory
    builder.Services.AddDbContext<AuthIdentityDbContext>(options =>
        options.UseInMemoryDatabase("AuthSecurityDb"));
}

builder.Services.AddIdentity<WatchUser, WatchRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 15;

    // Lockout (Item 77: brute force)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthIdentityDbContext>()
.AddDefaultTokenProviders();

// Register Argon2id password hasher (Item 62)
builder.Services.AddScoped<IPasswordHasher<WatchUser>, Argon2PasswordHasher>();

// JWT Authentication + Authorization policies
builder.Services.AddWatchJwtAuth(builder.Configuration);

// Rate Limiting (Item 74)
builder.Services.AddWatchRateLimiting();

// Hangfire with Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Item 305: Distributed cache for SMS OTP storage (Redis in production, in-memory in dev)
// In production, override with AddStackExchangeRedisCache() via a Redis connection string.
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
    builder.Services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache(); // dev/test fallback

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<MfaService>();
builder.Services.AddScoped<SmsMfaService>();
builder.Services.AddScoped<MagicLinkService>();
builder.Services.AddScoped<PasskeyService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<BruteForceDetectionService>();
builder.Services.AddScoped<DeviceTrustService>();
builder.Services.AddScoped<EulaService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<SessionManagementService>();
builder.Services.AddScoped<StrideThreatService>();
builder.Services.AddScoped<MitreDetectionService>();
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
builder.Services.AddWatchTracing("TheWatch.P5.AuthSecurity");
var app = builder.Build();

// Seed roles and MITRE rules
await SeedDataAsync(app.Services);

app.UseCors();
app.UseWatchMetrics();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseWatchSecurity(); // Rate limiter + security audit middleware (from SecurityGenerator)
app.UseAuthentication();
app.UseAuthorization();
// Item 231: ETag / If-None-Match conditional response support
app.UseWatchETagSupport();

// IP Throttling middleware (Item 75)
app.UseMiddleware<IpThrottlingMiddleware>();

// Sliding window token middleware (Item 67)
app.UseMiddleware<SlidingWindowTokenMiddleware>();

app.UseMiddleware<TheWatch.Shared.Security.WatchProblemDetailsMiddleware>();
app.UseMiddleware<TheWatch.Shared.Security.CuiMarkingMiddleware>();

app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new TheWatch.Shared.Security.HangfireDashboardAuthFilter()],
    IsReadOnlyFunc = _ => true
});
app.MapWatchControllers();

// Schedule recurring security jobs
RecurringJob.AddOrUpdate<StrideThreatService>("stride-threat-scan", s => s.ScanAsync(), "*/15 * * * *");
RecurringJob.AddOrUpdate<MitreDetectionService>("mitre-detection-scan", s => s.ScanAsync(), "*/15 * * * *");

// Health endpoint — stripped of internal details (DISA STIG V-222609)
// Item 246: Readiness probe — checks SQL Server, Redis, Kafka, PostGIS connectivity
app.MapHealthChecks("/health/ready");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// Service info — admin-only (DISA STIG V-222609: no unauthenticated service metadata)
app.MapGet("/info", () => new
{
    Service = "TheWatch.P5.AuthSecurity",
    Program = "P5",
    Name = "AuthSecurity",
    Description = "Authentication, MFA, JWT, RBAC, security monitoring",
    Icon = "shield",
    Version = "1.0.0"
}).RequireAuthorization(WatchPolicies.AdminOnly);

// === Auth Endpoints ===

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    try
    {
        var response = await auth.RegisterAsync(request);
        await audit.LogAsync("Register", response.User.Id, ctx, true, attemptedIdentity: request.Email);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        await audit.LogAsync("Register", null, ctx, false, ex.Message, attemptedIdentity: request.Email);
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    try
    {
        var response = await auth.LoginAsync(request);
        if (!response.MfaRequired)
            await audit.LogAsync("Login", response.User.Id, ctx, true, attemptedIdentity: request.Email);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException)
    {
        await audit.LogAsync("Login", null, ctx, false, "Invalid credentials", attemptedIdentity: request.Email);
        return Results.Unauthorized();
    }
});

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    try
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString();
        var tokens = await auth.RefreshAsync(request.RefreshToken, request.DeviceFingerprint, ip);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException)
    {
        await audit.LogAsync("RefreshToken", null, ctx, false, "Invalid or revoked refresh token");
        return Results.Unauthorized();
    }
});

app.MapGet("/api/auth/me", async (ClaimsPrincipal user, IAuthService auth) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub");
    if (sub is null || !Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var info = await auth.GetUserByIdAsync(userId);
    return info is not null ? Results.Ok(info) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/auth/users", async (IAuthService auth) =>
{
    var users = await auth.ListUsersAsync();
    return Results.Ok(users);
}).RequireAuthorization(WatchPolicies.AdminOnly);

// Item 71: Role assignment (admin only)
app.MapPost("/api/auth/roles/assign", async (AssignRoleRequest request, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    try
    {
        await auth.AssignRoleAsync(request.UserId, request.Role);
        await audit.LogAsync("AssignRole", request.UserId, ctx, true, $"Role: {request.Role}");
        return Results.Ok(new { message = $"Role {request.Role} assigned." });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization(WatchPolicies.AdminOnly);

// Item 299/300: Change password — 60-day expiry, 24-hour min age, ≥8 character-position delta (STIG V-222541/544/545)
app.MapPost("/api/auth/change-password", async (ChangePasswordRequest request, ClaimsPrincipal principal, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    var userId = GetUserId(principal);
    if (userId is null) return Results.Unauthorized();
    try
    {
        await auth.ChangePasswordAsync(userId.Value, request);
        await audit.LogAsync("ChangePassword", userId, ctx, true);
        return Results.Ok(new { message = "Password changed successfully." });
    }
    catch (InvalidOperationException ex)
    {
        await audit.LogAsync("ChangePassword", userId, ctx, false, ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// === MFA Endpoints (Items 63-66) ===

// TOTP MFA
app.MapPost("/api/auth/mfa/totp/enable", async (ClaimsPrincipal user, MfaService mfa) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var result = await mfa.EnableTotpAsync(userId.Value);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/api/auth/mfa/totp/verify", async (MfaVerifyRequest request, ClaimsPrincipal user, MfaService mfa) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var valid = await mfa.VerifyTotpAsync(userId.Value, request.Code);
    return valid ? Results.Ok(new { verified = true }) : Results.BadRequest(new { error = "Invalid code." });
}).RequireAuthorization();

app.MapPost("/api/auth/mfa/totp/disable", async (ClaimsPrincipal user, MfaService mfa) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    await mfa.DisableTotpAsync(userId.Value);
    return Results.Ok(new { disabled = true });
}).RequireAuthorization();

// SMS MFA
app.MapPost("/api/auth/mfa/sms/send", async (SmsMfaSendRequest request, SmsMfaService sms) =>
{
    var sent = await sms.SendCodeAsync(request.PhoneNumber);
    return sent ? Results.Ok(new { sent = true }) : Results.StatusCode(503);
}).RequireRateLimiting("MfaLimit");

app.MapPost("/api/auth/mfa/sms/verify", async (SmsMfaVerifyRequest request, SmsMfaService sms) =>
{
    var valid = await sms.VerifyCodeAsync(request.PhoneNumber, request.Code);
    return valid ? Results.Ok(new { verified = true }) : Results.BadRequest(new { error = "Invalid or expired code." });
});

// Magic Link
app.MapPost("/api/auth/magic-link/send", async (MagicLinkRequest request, MagicLinkService magic) =>
{
    await magic.SendMagicLinkAsync(request.Email);
    return Results.Ok(new { sent = true });
}).RequireRateLimiting("MfaLimit");

app.MapGet("/api/auth/magic-link/verify", async (string token, string email, MagicLinkService magic) =>
{
    var valid = await magic.VerifyMagicLinkAsync(email, token);
    return valid ? Results.Ok(new { verified = true }) : Results.BadRequest(new { error = "Invalid or expired magic link." });
});

// Passkey/FIDO2
app.MapPost("/api/auth/passkey/register/begin", async (ClaimsPrincipal user, PasskeyService passkey) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var options = await passkey.BeginRegistrationAsync(userId.Value);
    return Results.Ok(options);
}).RequireAuthorization();

app.MapPost("/api/auth/passkey/register/complete", async (JsonElement body, ClaimsPrincipal user, PasskeyService passkey) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var success = await passkey.CompleteRegistrationAsync(userId.Value, body);
    return success ? Results.Ok(new { registered = true }) : Results.BadRequest(new { error = "Registration failed." });
}).RequireAuthorization();

app.MapPost("/api/auth/passkey/authenticate/begin", async (PasskeyService passkey) =>
{
    var options = passkey.BeginAuthentication();
    return Results.Ok(options);
});

app.MapPost("/api/auth/passkey/authenticate/complete", async (JsonElement body, PasskeyService passkey, IAuthService auth) =>
{
    var userId = await passkey.CompleteAuthenticationAsync(body);
    if (userId is null) return Results.Unauthorized();
    var userInfo = await auth.GetUserByIdAsync(userId.Value);
    return userInfo is not null ? Results.Ok(userInfo) : Results.Unauthorized();
});

// === EULA Endpoints (Item 69) ===

app.MapGet("/api/eula/current", async (EulaService eula) =>
{
    var current = await eula.GetCurrentVersionAsync();
    return current is not null ? Results.Ok(current) : Results.NotFound();
});

app.MapPost("/api/eula/accept", async (ClaimsPrincipal user, EulaService eula, HttpContext ctx) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var ip = ctx.Connection.RemoteIpAddress?.ToString();
    await eula.AcceptCurrentVersionAsync(userId.Value, ip);
    return Results.Ok(new { accepted = true });
}).RequireAuthorization();

app.MapGet("/api/eula/status", async (ClaimsPrincipal user, EulaService eula) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var status = await eula.GetAcceptanceStatusAsync(userId.Value);
    return Results.Ok(status);
}).RequireAuthorization();

// === Onboarding Endpoints (Item 70) ===

app.MapGet("/api/onboarding/progress", async (ClaimsPrincipal user, OnboardingService onboarding) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var progress = await onboarding.GetProgressAsync(userId.Value);
    return Results.Ok(progress);
}).RequireAuthorization();

app.MapPost("/api/onboarding/complete-step", async (CompleteOnboardingStepRequest request, ClaimsPrincipal user, OnboardingService onboarding) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    // Step is validated by enum binding — invalid values produce a 400 automatically
    var step = request.Step.ToString().ToLowerInvariant().Replace("emergencycontacts", "emergency-contacts")
                                                          .Replace("notificationpreferences", "notification-preferences");
    await onboarding.CompleteStepAsync(userId.Value, step);
    return Results.Ok(new { completed = step });
}).RequireAuthorization();

app.MapPost("/api/onboarding/reset", async (ClaimsPrincipal user, OnboardingService onboarding) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    await onboarding.ResetAsync(userId.Value);
    return Results.Ok(new { reset = true });
}).RequireAuthorization();

// === Security Monitoring Endpoints (Items 76-80) ===

app.MapGet("/api/security/audit", async (int? page, int? pageSize, AuditService audit) =>
{
    var events = await audit.GetRecentEventsAsync(page ?? 1, pageSize ?? 50);
    return Results.Ok(events);
}).RequireAuthorization(WatchPolicies.AdminOnly);

app.MapGet("/api/security/threats", async (StrideThreatService stride) =>
{
    var threats = await stride.GetRecentThreatsAsync();
    return Results.Ok(threats);
}).RequireAuthorization(WatchPolicies.AdminOnly);

app.MapGet("/api/security/mitre/rules", async (MitreDetectionService mitre) =>
{
    var rules = await mitre.GetRulesAsync();
    return Results.Ok(rules);
}).RequireAuthorization(WatchPolicies.AdminOnly);

app.MapGet("/api/security/device-trust/{userId:guid}", async (Guid userId, DeviceTrustService deviceTrust) =>
{
    var devices = await deviceTrust.GetDevicesForUserAsync(userId);
    return Results.Ok(devices);
}).RequireAuthorization(WatchPolicies.AdminOnly);

app.Run();

// Helper
static Guid? GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    return Guid.TryParse(sub, out var id) ? id : null;
}

// Seed roles, admin, and MITRE rules
static async Task SeedDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthIdentityDbContext>();

    // Use MigrateAsync for SQL Server (production), EnsureCreatedAsync for InMemory (dev/test)
    if (db.Database.IsSqlServer())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WatchRole>>();
    foreach (var role in WatchRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new WatchRole(role, $"TheWatch {role} role"));
    }

    // Seed MITRE ATT&CK rules (Item 80)
    if (!await db.AttackDetectionRules.AnyAsync())
    {
        db.AttackDetectionRules.AddRange(
            new AttackDetectionRule { TechniqueId = "T1078", TechniqueName = "Valid Accounts (Credential Stuffing)", Tactic = "Initial Access", ThresholdCount = 10, TimeWindowMinutes = 5, Severity = "High" },
            new AttackDetectionRule { TechniqueId = "T1110", TechniqueName = "Brute Force", Tactic = "Credential Access", ThresholdCount = 20, TimeWindowMinutes = 10, Severity = "High" },
            new AttackDetectionRule { TechniqueId = "T1528", TechniqueName = "Steal Application Access Token", Tactic = "Credential Access", ThresholdCount = 3, TimeWindowMinutes = 15, Severity = "Critical" },
            new AttackDetectionRule { TechniqueId = "T1621", TechniqueName = "MFA Fatigue / Push Notification Spam", Tactic = "Credential Access", ThresholdCount = 5, TimeWindowMinutes = 5, Severity = "High" },
            new AttackDetectionRule { TechniqueId = "T1556", TechniqueName = "Modify Authentication Process", Tactic = "Persistence", ThresholdCount = 1, TimeWindowMinutes = 60, Severity = "Critical" }
        );
        await db.SaveChangesAsync();
    }

    // Seed initial EULA version
    if (!await db.EulaVersions.AnyAsync())
    {
        db.EulaVersions.Add(new EulaVersion
        {
            Version = "1.0.0",
            Content = "TheWatch End User License Agreement v1.0.0. By using this service you agree to the terms.",
            IsCurrent = true
        });
        await db.SaveChangesAsync();
    }
}

// Needed for WebApplicationFactory in tests
public partial class Program { }
