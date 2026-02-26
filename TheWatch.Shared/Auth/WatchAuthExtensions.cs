using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TheWatch.Shared.Auth;

/// <summary>
/// Shared JWT Bearer + authorization policy setup for all services.
/// </summary>
public static class WatchAuthExtensions
{
    public static IServiceCollection AddWatchJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "TheWatch-P5-AuthSecurity-DevKey-Min32Chars!!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TheWatch";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Support SignalR token via query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(WatchPolicies.AdminOnly, policy =>
                policy.RequireRole(WatchRoles.Admin))
            .AddPolicy(WatchPolicies.ResponderAccess, policy =>
                policy.RequireRole(WatchRoles.Admin, WatchRoles.Responder))
            .AddPolicy(WatchPolicies.DoctorAccess, policy =>
                policy.RequireRole(WatchRoles.Admin, WatchRoles.Doctor))
            .AddPolicy(WatchPolicies.FamilyAccess, policy =>
                policy.RequireRole(WatchRoles.Admin, WatchRoles.FamilyMember, WatchRoles.Patient))
            .AddPolicy(WatchPolicies.ServiceToService, policy =>
                policy.RequireRole(WatchRoles.Admin, WatchRoles.ServiceAccount))
            .AddPolicy(WatchPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser());

        return services;
    }
}
