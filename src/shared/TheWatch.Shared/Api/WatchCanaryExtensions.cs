using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TheWatch.Shared.Api;

/// <summary>
/// Item 249: Canary endpoints for synthetic monitoring and smoke testing.
/// Maps lightweight endpoints that load balancers, uptime monitors, and canary
/// pipelines can hit to verify a service is alive and its dependencies are reachable.
/// </summary>
/// <remarks>
/// <para>
/// Two endpoints are registered:
/// <list type="bullet">
///   <item><c>GET /canary</c> - Shallow check returning service name, assembly
///     version, current UTC timestamp, and process uptime.</item>
///   <item><c>GET /canary/deep</c> - Deep check that additionally probes DB
///     connectivity (via EF Core health checks), Redis, and Kafka availability
///     reported through the ASP.NET Core <see cref="HealthCheckService"/>.</item>
/// </list>
/// </para>
/// <para>
/// Usage in Program.cs:
/// <code>
/// app.MapWatchCanaryEndpoints("TheWatch.P1.CoreGateway");
/// </code>
/// </para>
/// </remarks>
public static class WatchCanaryExtensions
{
    private static readonly DateTime ProcessStartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    /// <summary>
    /// Maps canary endpoints (<c>/canary</c> and <c>/canary/deep</c>) for the given service.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (typically <see cref="WebApplication"/>).</param>
    /// <param name="serviceName">
    /// The logical name of the service (e.g. "TheWatch.P1.CoreGateway").
    /// If null, the entry assembly name is used.
    /// </param>
    /// <returns>The <paramref name="endpoints"/> instance for chaining.</returns>
    public static IEndpointRouteBuilder MapWatchCanaryEndpoints(this IEndpointRouteBuilder endpoints, string? serviceName = null)
    {
        var name = serviceName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
        var version = Assembly.GetEntryAssembly()?.GetInformationalVersion()
                   ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                   ?? "0.0.0";

        // ----------------------------------------------------------------
        // GET /canary  -  shallow health probe
        // ----------------------------------------------------------------
        endpoints.MapGet("/canary", () =>
        {
            var uptime = DateTime.UtcNow - ProcessStartTime;

            return Results.Ok(new CanaryResponse
            {
                Service = name,
                Version = version,
                Timestamp = DateTime.UtcNow,
                UptimeSeconds = (long)uptime.TotalSeconds,
                Uptime = FormatUptime(uptime),
                Status = "Healthy"
            });
        })
        .WithTags("Canary")
        .WithName($"{name}_Canary")
        .ExcludeFromDescription(); // Exclude from OpenAPI docs

        // ----------------------------------------------------------------
        // GET /canary/deep  -  deep dependency check
        // ----------------------------------------------------------------
        endpoints.MapGet("/canary/deep", async (HttpContext context) =>
        {
            var uptime = DateTime.UtcNow - ProcessStartTime;
            var config = context.RequestServices.GetService<IConfiguration>();

            var dependencies = new Dictionary<string, DependencyStatus>();

            // Check database connectivity
            dependencies["database"] = await CheckDatabaseAsync(context);

            // Check Redis connectivity
            dependencies["redis"] = await CheckRedisAsync(context, config);

            // Check Kafka connectivity
            dependencies["kafka"] = await CheckKafkaAsync(context, config);

            // Check registered health checks (ASP.NET Core HealthCheckService)
            await CheckHealthChecksAsync(context, dependencies);

            var allHealthy = dependencies.Values.All(d =>
                d.Status == "Healthy" || d.Status == "NotConfigured");

            return Results.Ok(new CanaryDeepResponse
            {
                Service = name,
                Version = version,
                Timestamp = DateTime.UtcNow,
                UptimeSeconds = (long)uptime.TotalSeconds,
                Uptime = FormatUptime(uptime),
                Status = allHealthy ? "Healthy" : "Degraded",
                Dependencies = dependencies
            });
        })
        .WithTags("Canary")
        .WithName($"{name}_CanaryDeep")
        .ExcludeFromDescription();

        return endpoints;
    }

    // ------------------------------------------------------------------
    // Dependency checks
    // ------------------------------------------------------------------

    private static async Task<DependencyStatus> CheckDatabaseAsync(HttpContext context)
    {
        try
        {
            // Attempt to resolve a DbContext and check connectivity
            // We look for the ASP.NET Core health check results first
            var healthService = context.RequestServices.GetService<HealthCheckService>();
            if (healthService is not null)
            {
                var report = await healthService.CheckHealthAsync(
                    r => r.Tags.Contains("db") || r.Tags.Contains("database") || r.Tags.Contains("ef"),
                    context.RequestAborted);

                if (report.Entries.Count > 0)
                {
                    var worstStatus = report.Status;
                    return new DependencyStatus
                    {
                        Status = worstStatus == HealthStatus.Healthy ? "Healthy"
                               : worstStatus == HealthStatus.Degraded ? "Degraded"
                               : "Unhealthy",
                        LatencyMs = report.TotalDuration.TotalMilliseconds,
                        Details = report.Entries.ToDictionary(
                            e => e.Key,
                            e => e.Value.Description ?? e.Value.Status.ToString())
                    };
                }
            }

            return new DependencyStatus { Status = "NotConfigured" };
        }
        catch (Exception ex)
        {
            return new DependencyStatus
            {
                Status = "Unhealthy",
                Details = new Dictionary<string, string> { ["error"] = ex.Message }
            };
        }
    }

    private static async Task<DependencyStatus> CheckRedisAsync(HttpContext context, IConfiguration? config)
    {
        try
        {
            var redisConnection = config?.GetConnectionString("redis")
                               ?? config?["Redis:ConnectionString"];

            if (string.IsNullOrEmpty(redisConnection))
                return new DependencyStatus { Status = "NotConfigured" };

            // Check via health check service if available
            var healthService = context.RequestServices.GetService<HealthCheckService>();
            if (healthService is not null)
            {
                var report = await healthService.CheckHealthAsync(
                    r => r.Tags.Contains("redis") || r.Tags.Contains("cache"),
                    context.RequestAborted);

                if (report.Entries.Count > 0)
                {
                    return new DependencyStatus
                    {
                        Status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
                        LatencyMs = report.TotalDuration.TotalMilliseconds
                    };
                }
            }

            return new DependencyStatus { Status = "Configured", Details = new Dictionary<string, string> { ["note"] = "No health check registered" } };
        }
        catch (Exception ex)
        {
            return new DependencyStatus
            {
                Status = "Unhealthy",
                Details = new Dictionary<string, string> { ["error"] = ex.Message }
            };
        }
    }

    private static async Task<DependencyStatus> CheckKafkaAsync(HttpContext context, IConfiguration? config)
    {
        try
        {
            var kafkaServers = config?["Kafka:BootstrapServers"]
                            ?? config?.GetConnectionString("kafka");

            if (string.IsNullOrEmpty(kafkaServers))
                return new DependencyStatus { Status = "NotConfigured" };

            // Check via health check service if available
            var healthService = context.RequestServices.GetService<HealthCheckService>();
            if (healthService is not null)
            {
                var report = await healthService.CheckHealthAsync(
                    r => r.Tags.Contains("kafka") || r.Tags.Contains("messaging"),
                    context.RequestAborted);

                if (report.Entries.Count > 0)
                {
                    return new DependencyStatus
                    {
                        Status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
                        LatencyMs = report.TotalDuration.TotalMilliseconds
                    };
                }
            }

            return new DependencyStatus { Status = "Configured", Details = new Dictionary<string, string> { ["note"] = "No health check registered" } };
        }
        catch (Exception ex)
        {
            return new DependencyStatus
            {
                Status = "Unhealthy",
                Details = new Dictionary<string, string> { ["error"] = ex.Message }
            };
        }
    }

    private static async Task CheckHealthChecksAsync(HttpContext context, Dictionary<string, DependencyStatus> dependencies)
    {
        try
        {
            var healthService = context.RequestServices.GetService<HealthCheckService>();
            if (healthService is null)
                return;

            var report = await healthService.CheckHealthAsync(context.RequestAborted);

            foreach (var entry in report.Entries)
            {
                // Skip entries that are already represented by explicit checks above
                var key = entry.Key.ToLowerInvariant();
                if (dependencies.ContainsKey(key))
                    continue;

                dependencies[entry.Key] = new DependencyStatus
                {
                    Status = entry.Value.Status == HealthStatus.Healthy ? "Healthy"
                           : entry.Value.Status == HealthStatus.Degraded ? "Degraded"
                           : "Unhealthy",
                    LatencyMs = entry.Value.Duration.TotalMilliseconds,
                    Details = !string.IsNullOrEmpty(entry.Value.Description)
                        ? new Dictionary<string, string> { ["description"] = entry.Value.Description }
                        : null
                };
            }
        }
        catch
        {
            // Health check service unavailable - skip
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        if (uptime.TotalHours >= 1)
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        if (uptime.TotalMinutes >= 1)
            return $"{uptime.Minutes}m {uptime.Seconds}s";
        return $"{uptime.Seconds}s";
    }

    // ------------------------------------------------------------------
    // Response models
    // ------------------------------------------------------------------

    /// <summary>Shallow canary response.</summary>
    public class CanaryResponse
    {
        public string Service { get; init; } = default!;
        public string Version { get; init; } = default!;
        public DateTime Timestamp { get; init; }
        public long UptimeSeconds { get; init; }
        public string Uptime { get; init; } = default!;
        public string Status { get; init; } = default!;
    }

    /// <summary>Deep canary response including dependency health.</summary>
    public class CanaryDeepResponse : CanaryResponse
    {
        public Dictionary<string, DependencyStatus> Dependencies { get; init; } = new();
    }

    /// <summary>Status of an individual dependency.</summary>
    public class DependencyStatus
    {
        public string Status { get; init; } = "Unknown";
        public double? LatencyMs { get; init; }
        public Dictionary<string, string>? Details { get; init; }
    }
}
