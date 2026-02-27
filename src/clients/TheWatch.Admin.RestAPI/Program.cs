using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TheWatch.Admin.RestAPI.Auth;
using TheWatch.Admin.RestAPI.Middleware;
using TheWatch.Aspire.ServiceDefaults;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.MeshNetwork;
using TheWatch.Contracts.Wearable;
using TheWatch.Contracts.AuthSecurity;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.FamilyHealth;
using TheWatch.Contracts.DisasterRelief;
using TheWatch.Contracts.DoctorServices;
using TheWatch.Contracts.Gamification;
using TheWatch.Contracts.Geospatial;
using TheWatch.Contracts.Surveillance;
using TheWatch.Contracts.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// ──────── Security+ 2.1: Request Size Limits (DoS mitigation) ────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB max
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB headers
    options.Limits.MaxRequestLineSize = 8192;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.AddServerHeader = false; // Security+ 2.1: Don't leak server identity
});

// ──────── Security+ 1.3: JWT Authentication (AAA) ────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("FATAL: Jwt:Key not configured. Set the Jwt:Key configuration value.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TheWatch";
var clockSkewSeconds = int.TryParse(builder.Configuration["Jwt:ClockSkewSeconds"], out var cs) ? cs : 30;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(clockSkewSeconds),
            // Security+ 3.1: Require signed tokens, reject unsigned
            RequireSignedTokens = true,
            RequireExpirationTime = true
        };

        // Security+ 4.1: Log authentication failures for SIEM
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = context.HttpContext.Request.Path.Value ?? "/";

                logger.LogWarning("[SEC:AUTH_FAILED] Path={Path} IP={IP} Error={Error}",
                    path, ip, context.Exception.GetType().Name);

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = context.HttpContext.Request.Path.Value ?? "/";

                if (!context.Handled)
                {
                    logger.LogWarning("[SEC:AUTH_CHALLENGE] Path={Path} IP={IP} Error={Error} Description={Description}",
                        path, ip, context.Error, context.ErrorDescription);
                }

                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var sub = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

                logger.LogWarning("[SEC:FORBIDDEN] Path={Path} IP={IP} Sub={Sub}",
                    context.HttpContext.Request.Path, ip, sub);

                return Task.CompletedTask;
            }
        };
    });

// Security+ 1.3: Authorization policies with explicit fallback
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"))
    .AddPolicy("ResponderAccess", policy =>
        policy.RequireRole("Admin", "Responder"))
    .AddPolicy("DoctorAccess", policy =>
        policy.RequireRole("Admin", "Doctor"))
    .AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser())
    // Security+ 3.2: Fallback — require auth by default, deny anonymous unless explicitly allowed
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// ──────── Security+ 2.1: CORS (Restrictive — Least Privilege) ────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["https://localhost:5001"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id", "Accept")
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// ──────── Security+ 2.3: Rate Limiting (Brute Force Mitigation) ──────
var globalPermit = int.TryParse(builder.Configuration["RateLimiting:GlobalPermitLimit"], out var gp) ? gp : 100;
var globalWindow = int.TryParse(builder.Configuration["RateLimiting:GlobalWindowMinutes"], out var gw) ? gw : 1;
var authPermit = int.TryParse(builder.Configuration["RateLimiting:AuthPermitLimit"], out var ap) ? ap : 10;
var authWindow = int.TryParse(builder.Configuration["RateLimiting:AuthWindowMinutes"], out var aw) ? aw : 1;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Security+ 4.1: Log rate limit rejections
    options.OnRejected = async (context, ct) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        logger.LogWarning("[SEC:RATE_LIMITED] IP={IP} Path={Path} Lease={Lease}",
            ip, context.HttpContext.Request.Path, context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter.TotalSeconds : 60);

        context.HttpContext.Response.Headers["Retry-After"] =
            retryAfter.TotalSeconds > 0 ? ((int)retryAfter.TotalSeconds).ToString() : "60";
    };

    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = globalPermit;
        opt.Window = TimeSpan.FromMinutes(globalWindow);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = authPermit;
        opt.Window = TimeSpan.FromMinutes(authWindow);
        opt.QueueLimit = 0;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/admin/auth/login") || path.StartsWith("/api/admin/auth/register")
            || path.StartsWith("/api/admin/auth/refresh"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authPermit,
                    Window = TimeSpan.FromMinutes(authWindow)
                });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalPermit,
                Window = TimeSpan.FromMinutes(globalWindow)
            });
    });
});

// ──────── Infrastructure ─────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TokenPropagationHandler>();
builder.Services.AddSingleton<ServiceTokenProvider>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Security+ 3.1: HSTS for transport security
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// ──────── Typed HTTP Clients (12 services) ───────────────────────────
// Item 212: Wire all typed clients with resilience (217), correlation ID (218), and API key auth (219)
void ConfigureClient(IHttpClientBuilder clientBuilder, string aspireServiceName)
{
    clientBuilder
        .ConfigureHttpClient(c => c.BaseAddress = new Uri($"https+http://{aspireServiceName}"))
        .AddHttpMessageHandler<TokenPropagationHandler>()
        .AddServiceDiscovery()
        .AddStandardResilienceHandler();
}

ConfigureClient(builder.Services.AddCoreGatewayClient(), "p1-coregateway");
ConfigureClient(builder.Services.AddVoiceEmergencyClient(), "p2-voiceemergency");
ConfigureClient(builder.Services.AddMeshNetworkClient(), "p3-meshnetwork");
ConfigureClient(builder.Services.AddWearableClient(), "p4-wearable");
ConfigureClient(builder.Services.AddAuthSecurityClient(), "p5-authsecurity");
ConfigureClient(builder.Services.AddFirstResponderClient(), "p6-firstresponder");
ConfigureClient(builder.Services.AddFamilyHealthClient(), "p7-familyhealth");
ConfigureClient(builder.Services.AddDisasterReliefClient(), "p8-disasterrelief");
ConfigureClient(builder.Services.AddDoctorServicesClient(), "p9-doctorservices");
ConfigureClient(builder.Services.AddGamificationClient(), "p10-gamification");
ConfigureClient(builder.Services.AddGeospatialClient(), "geospatial");
ConfigureClient(builder.Services.AddSurveillanceClient(), "p11-surveillance");
ConfigureClient(builder.Services.AddNotificationsClient(), "p12-notifications");

// ──────── App Pipeline (Security+ 3.1: Defense in Depth layers) ─────
var app = builder.Build();

// Security+ 3.1: HTTPS redirect + HSTS (transport security)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// Middleware order: security headers → correlation → rate limit → exception → CORS → auth → zero trust
// Security+ 2.3: GlobalExceptionMiddleware AFTER RateLimiter so 429 responses aren't caught/masked
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ZeroTrustMiddleware>();

// Security+ 3.2: OpenAPI only in development (attack surface reduction)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
