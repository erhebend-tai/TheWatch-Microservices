using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Azure;

/// <summary>
/// Extensions to layer Application Insights alongside existing Serilog telemetry.
/// This is ADDITIVE — Serilog continues to function for console/file logging.
/// Application Insights adds: distributed tracing, live metrics, dependency tracking,
/// exception snapshots, and custom metrics to Azure Monitor.
///
/// Usage in Program.cs:
///   builder.ConfigureWatchSerilog();  // existing Serilog setup
///   builder.AddApplicationInsightsIfConfigured();  // layers App Insights on top
///
/// Toggle via Azure:UseApplicationInsights = true in appsettings.json.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// If Azure:UseApplicationInsights is true and a connection string is provided,
    /// adds Application Insights telemetry alongside existing Serilog.
    /// Otherwise, no-op — Serilog remains the sole telemetry provider.
    /// </summary>
    public static IHostApplicationBuilder AddApplicationInsightsIfConfigured(
        this IHostApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(AzureServiceOptions.SectionName)
            .Get<AzureServiceOptions>();

        if (options is not { UseApplicationInsights: true })
            return builder;

        var connectionString = options.ApplicationInsightsConnectionString
            ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            return builder;

        // Add Application Insights telemetry collection.
        // This works alongside OpenTelemetry (from ServiceDefaults) and Serilog.
        builder.Services.AddApplicationInsightsTelemetry(aiOptions =>
        {
            aiOptions.ConnectionString = connectionString;
            aiOptions.EnableAdaptiveSampling = true;
            aiOptions.EnableDependencyTrackingTelemetryModule = true;
            aiOptions.EnableRequestTrackingTelemetryModule = true;
            aiOptions.EnablePerformanceCounterCollectionModule = true;
        });

        // Add Application Insights as an ILogger provider so Serilog + AI coexist.
        builder.Logging.AddApplicationInsights(
            configureTelemetryConfiguration: config =>
            {
                config.ConnectionString = connectionString;
            },
            configureApplicationInsightsLoggerOptions: _ => { });

        return builder;
    }

    /// <summary>
    /// Overload for WebApplicationBuilder (most common in Program.cs).
    /// </summary>
    public static WebApplicationBuilder AddApplicationInsightsIfConfigured(
        this WebApplicationBuilder builder)
    {
        ((IHostApplicationBuilder)builder).AddApplicationInsightsIfConfigured();
        return builder;
    }
}
