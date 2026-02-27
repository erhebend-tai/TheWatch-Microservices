using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TheWatch.Shared.Monitoring;

namespace TheWatch.Shared.Observability;

/// <summary>
/// Item 244: Extension methods for registering TheWatch Prometheus metrics and
/// the metrics-recording middleware in every microservice.
/// </summary>
/// <remarks>
/// <para>
/// Usage in Program.cs:
/// <code>
/// // Service registration
/// builder.Services.AddWatchMetrics();
///
/// var app = builder.Build();
///
/// // Middleware pipeline (register before auth)
/// app.UseWatchMetrics();
/// </code>
/// </para>
/// <para>
/// This registers both the <see cref="IWatchPrometheusMetrics"/> (Prometheus-oriented
/// counters, histograms, gauges) and the existing <see cref="IWatchMetrics"/>
/// (business-level metrics) as singletons. The <see cref="WatchMetricsMiddleware"/>
/// is added to the pipeline to automatically instrument every HTTP request.
/// </para>
/// </remarks>
public static class WatchObservabilityExtensions
{
    /// <summary>
    /// Registers TheWatch metrics services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchMetrics(this IServiceCollection services)
    {
        // Prometheus-oriented metrics (Item 244)
        services.AddSingleton<IWatchPrometheusMetrics, WatchPrometheusMetrics>();

        // Business-level metrics (existing, from TheWatch.Shared.Monitoring)
        services.AddSingleton<IWatchMetrics, WatchMetrics>();

        return services;
    }

    /// <summary>
    /// Adds the <see cref="WatchMetricsMiddleware"/> to the pipeline, which records
    /// request duration, status code, route, and auth failure metrics for every
    /// HTTP request passing through the service.
    /// </summary>
    /// <param name="app">The application builder pipeline.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    /// <remarks>
    /// Register this middleware early in the pipeline, after exception handling and
    /// CORS but before authentication, so that the recorded duration includes
    /// authentication and business logic time:
    /// <code>
    /// app.UseWatchExceptionHandler();
    /// app.UseCors();
    /// app.UseWatchMetrics();          // &lt;-- here
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// </code>
    /// </remarks>
    public static IApplicationBuilder UseWatchMetrics(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WatchMetricsMiddleware>();
    }
}
