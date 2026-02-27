using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Shared.Security;

/// <summary>
/// Security+ 2.1: Centralized CORS configuration. Reads allowed origins from config,
/// restricts methods/headers, and optionally supports SignalR credentials.
/// </summary>
public static class WatchCorsExtensions
{
    public static IServiceCollection AddWatchCors(this IServiceCollection services,
        IConfiguration configuration, bool requiresSignalR = false)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? ["https://localhost:5001", "https://localhost:7001"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (requiresSignalR)
                {
                    // SignalR requires AllowCredentials which is incompatible with AllowAnyOrigin
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id",
                                       "X-Requested-With", "Accept", "X-SignalR-User-Agent")
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
                else
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id",
                                       "X-Requested-With", "Accept")
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
            });
        });

        return services;
    }
}
