using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TheWatch.P5.AuthSecurity;
using TheWatch.P5.AuthSecurity.Auth;
using TheWatch.P5.AuthSecurity.Services;
using TheWatch.Shared.Contracts;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TheWatch-P5-AuthSecurity-DevKey-Min32Chars!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TheWatch";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Hangfire
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Services
builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();

app.UseCors();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P5.AuthSecurity",
    "P5",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P5.AuthSecurity",
    Program = "P5",
    Name = "AuthSecurity",
    Description = "Authentication, MFA, JWT, security",
    Icon = "shield",
    Version = "0.2.0"
});

// === Auth Endpoints ===

app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService auth) =>
{
    try
    {
        var response = await auth.RegisterAsync(request);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth) =>
{
    try
    {
        var response = await auth.LoginAsync(request);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, IAuthService auth) =>
{
    try
    {
        var tokens = await auth.RefreshAsync(request.RefreshToken);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException)
    {
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
}).RequireAuthorization();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
