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
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;

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

var app = builder.Build();

// Seed roles and MITRE rules
await SeedDataAsync(app.Services);

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseWatchSecurity(); // Rate limiter + security audit middleware (from SecurityGenerator)
app.UseAuthentication();
app.UseAuthorization();

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
        await audit.LogAsync("Register", response.User.Id, ctx, true);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        await audit.LogAsync("Register", null, ctx, false, ex.Message);
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth, AuditService audit, HttpContext ctx) =>
{
    try
    {
        var response = await auth.LoginAsync(request);
        if (!response.MfaRequired)
            await audit.LogAsync("Login", response.User.Id, ctx, true);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException)
    {
        await audit.LogAsync("Login", null, ctx, false, "Invalid credentials");
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
    var valid = sms.VerifyCode(request.PhoneNumber, request.Code);
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

app.MapPost("/api/eula/versions", async (PublishEulaRequest request, EulaService eula) =>
{
    if (string.IsNullOrWhiteSpace(request.Version) || string.IsNullOrWhiteSpace(request.Content))
        return Results.BadRequest(new { error = "Version and content are required." });
    var version = await eula.PublishVersionAsync(request.Version, request.Content);
    return Results.Ok(version);
}).RequireAuthorization(WatchPolicies.AdminOnly);

// === Onboarding Endpoints (Item 70) ===

app.MapGet("/api/onboarding/progress", async (ClaimsPrincipal user, OnboardingService onboarding) =>
{
    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
    var progress = await onboarding.GetProgressAsync(userId.Value);
    return Results.Ok(progress);
}).RequireAuthorization();

app.MapPost("/api/onboarding/complete-step", async (string step, ClaimsPrincipal user, OnboardingService onboarding) =>
{
    var validSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "profile", "eula", "mfa", "emergency-contacts", "notification-preferences", "tutorial"
    };
    if (!validSteps.Contains(step))
        return Results.BadRequest(new { error = "Invalid onboarding step." });

    var userId = GetUserId(user);
    if (userId is null) return Results.Unauthorized();
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
            Content = """
                THEWATCH END USER LICENSE AGREEMENT
                Version 1.0.0 — Effective Date: January 1, 2025

                PLEASE READ THIS END USER LICENSE AGREEMENT ("AGREEMENT") CAREFULLY BEFORE USING THEWATCH
                ("APPLICATION"). BY ACCESSING OR USING THE APPLICATION, YOU AGREE TO BE BOUND BY THE TERMS
                OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT USE THE APPLICATION.

                1. GRANT OF LICENSE
                Subject to the terms of this Agreement, TheWatch grants you a limited, non-exclusive,
                non-transferable, revocable license to use the Application solely for lawful emergency
                coordination and personal safety purposes.

                2. RESTRICTIONS
                You may not: (a) copy, modify, or distribute the Application; (b) reverse engineer or
                attempt to extract source code; (c) use the Application for any unlawful purpose or in
                violation of any applicable laws or regulations; (d) share your account credentials with
                any third party; (e) use the Application to transmit false emergency alerts.

                3. EMERGENCY SERVICES DISCLAIMER
                THE APPLICATION IS AN AUXILIARY COMMUNICATION TOOL AND DOES NOT REPLACE OFFICIAL EMERGENCY
                SERVICES. ALWAYS DIAL YOUR LOCAL EMERGENCY NUMBER (e.g., 911) FOR LIFE-THREATENING
                SITUATIONS. THEWATCH MAKES NO WARRANTY THAT THE APPLICATION WILL BE AVAILABLE AT ALL
                TIMES OR IN ALL LOCATIONS.

                4. PRIVACY AND DATA COLLECTION
                The Application may collect location data, device information, health metrics, and
                communication logs solely to provide emergency coordination services. Your data is
                processed in accordance with our Privacy Policy. By using the Application you consent
                to this data processing.

                5. DATA SECURITY
                TheWatch employs industry-standard security controls including end-to-end encryption,
                multi-factor authentication, and regular security audits. You are responsible for
                maintaining the confidentiality of your account credentials.

                6. LOCATION SERVICES
                Emergency features require access to your device's location services. By enabling these
                features you consent to the continuous or periodic collection and transmission of your
                precise geographic location to emergency responders and designated contacts.

                7. HEALTH DATA
                If you use wearable integration or health monitoring features, you consent to the
                collection and processing of health-related data. This data may be shared with
                designated emergency contacts and first responders during an active emergency.

                8. INTELLECTUAL PROPERTY
                The Application and all content therein are the exclusive property of TheWatch and
                its licensors. No rights or licenses are granted except as expressly set forth herein.

                9. DISCLAIMER OF WARRANTIES
                THE APPLICATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND. THEWATCH DISCLAIMS
                ALL WARRANTIES, EXPRESS OR IMPLIED, INCLUDING WARRANTIES OF MERCHANTABILITY, FITNESS
                FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT.

                10. LIMITATION OF LIABILITY
                TO THE MAXIMUM EXTENT PERMITTED BY LAW, THEWATCH SHALL NOT BE LIABLE FOR ANY INDIRECT,
                INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES ARISING FROM YOUR USE OF THE
                APPLICATION.

                11. GOVERNING LAW
                This Agreement shall be governed by and construed in accordance with applicable law.
                Any disputes shall be resolved through binding arbitration.

                12. CHANGES TO THIS AGREEMENT
                TheWatch reserves the right to update this Agreement at any time. Continued use of the
                Application following notification of changes constitutes acceptance of the revised Agreement.

                13. CONTACT
                For questions about this Agreement, contact: legal@thewatch.app
                """,
            IsCurrent = true
        });
        await db.SaveChangesAsync();
    }
}

// Needed for WebApplicationFactory in tests
public partial class Program { }

internal record PublishEulaRequest(string Version, string Content);
