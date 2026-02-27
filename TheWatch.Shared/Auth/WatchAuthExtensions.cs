using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TheWatch.Shared.Auth;

/// <summary>
/// Shared JWT Bearer + authorization policy setup for all services.
/// Supports asymmetric RSA-2048/ECDSA (production) and symmetric HMAC (dev fallback).
/// NIST SC-12/SC-13, DISA STIG V-222641.
/// </summary>
public static class WatchAuthExtensions
{
    public static IServiceCollection AddWatchJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TheWatch.P5.AuthSecurity";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TheWatch";
        var asymmetricKeyPath = configuration["Jwt:AsymmetricPublicKeyPath"];

        SecurityKey signingKey;
        if (!string.IsNullOrEmpty(asymmetricKeyPath) && System.IO.File.Exists(asymmetricKeyPath))
        {
            // Production: RSA-2048 asymmetric validation (public key only at consumers)
            var rsa = RSA.Create();
            rsa.ImportFromPem(System.IO.File.ReadAllText(asymmetricKeyPath));
            signingKey = new RsaSecurityKey(rsa);
        }
        else
        {
            // Dev fallback: symmetric HMAC-SHA256
            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("FATAL: Neither Jwt:AsymmetricPublicKeyPath nor Jwt:Key configured.");
            signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        }

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
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30),
                RequireExpirationTime = true
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
