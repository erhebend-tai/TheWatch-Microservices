using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace TheWatch.Shared.Observability;

/// <summary>
/// Configures OpenTelemetry distributed tracing with Watch-specific span enrichment.
/// Registers HTTP, ASP.NET Core, SQL Client instrumentation and the
/// <see cref="WatchActivityEnricher"/> as a span processor that tags every span
/// with correlation, user, device, and incident identifiers from request headers.
/// </summary>
/// <remarks>
/// <para>
/// Usage in <c>Program.cs</c>:
/// <code>
/// builder.Services.AddWatchTracing("TheWatch.P2.VoiceEmergency");
/// </code>
/// </para>
/// <para>
/// This supplements (does not replace) the Aspire ServiceDefaults OpenTelemetry
/// configuration. If Aspire is not in use, this provides standalone tracing setup.
/// </para>
/// </remarks>
public static class WatchTracingExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing with Watch-specific instrumentation and span enrichment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">
    /// The logical service name (e.g., "TheWatch.P2.VoiceEmergency"). Used as the
    /// <c>service.name</c> resource attribute and as an ActivitySource name.
    /// </param>
    /// <param name="serviceVersion">
    /// Optional service version. Defaults to the calling assembly version.
    /// </param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchTracing(
        this IServiceCollection services,
        string serviceName,
        string? serviceVersion = null)
    {
        // Ensure IHttpContextAccessor is available for the enricher
        services.AddHttpContextAccessor();

        // Register the enricher as a singleton for reuse across spans
        services.AddSingleton(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return new WatchActivityEnricher(httpContextAccessor, serviceName, serviceVersion);
        });

        // Register the Activity processor that drives the enricher
        services.AddSingleton(sp =>
        {
            var enricher = sp.GetRequiredService<WatchActivityEnricher>();
            return new WatchActivityProcessor(enricher);
        });

        // Configure OpenTelemetry tracing
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    // Activity sources for the service and shared libraries
                    .AddSource(serviceName)
                    .AddSource("TheWatch.Shared")

                    // ASP.NET Core HTTP server instrumentation
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Enrich server spans with Watch context
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            EnrichFromRequest(activity, request);
                        };
                    })

                    // Outbound HTTP client instrumentation
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Propagate Watch headers to downstream spans
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            if (activity is not null)
                            {
                                activity.SetTag("watch.http.method", request.Method.Method);
                                activity.SetTag("watch.http.url", request.RequestUri?.ToString());
                            }
                        };
                    })

                    // SQL Client instrumentation for database query tracing
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })

                    // Register the custom Watch span processor.
                    // The processor enriches every span with service.name and service.version
                    // tags via the WatchActivityEnricher, so we do not need SetResourceBuilder here.
                    // When used with Aspire ServiceDefaults, the resource builder is already
                    // configured via ConfigureOpenTelemetry() in Extensions.cs.
                    .AddProcessor(sp => sp.GetRequiredService<WatchActivityProcessor>());
            });

        return services;
    }

    /// <summary>
    /// Enriches an Activity with Watch-specific headers from an HttpRequest.
    /// </summary>
    private static void EnrichFromRequest(Activity activity, HttpRequest request)
    {
        if (activity is null || request is null) return;

        SetTagFromHeader(activity, request, WatchActivityEnricher.Headers.CorrelationId, WatchActivityEnricher.Tags.CorrelationId);
        SetTagFromHeader(activity, request, WatchActivityEnricher.Headers.UserId, WatchActivityEnricher.Tags.UserId);
        SetTagFromHeader(activity, request, WatchActivityEnricher.Headers.DeviceId, WatchActivityEnricher.Tags.DeviceId);
        SetTagFromHeader(activity, request, WatchActivityEnricher.Headers.IncidentId, WatchActivityEnricher.Tags.IncidentId);
    }

    private static void SetTagFromHeader(Activity activity, HttpRequest request, string header, string tag)
    {
        if (request.Headers.TryGetValue(header, out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                activity.SetTag(tag, value);
            }
        }
    }
}

/// <summary>
/// OpenTelemetry <see cref="BaseProcessor{Activity}"/> that invokes
/// <see cref="WatchActivityEnricher.Enrich"/> on every span at start time.
/// This ensures all spans (including those not originating from HTTP requests)
/// get service identity tags, while HTTP-originating spans also get
/// correlation/user/device/incident tags.
/// </summary>
internal sealed class WatchActivityProcessor : BaseProcessor<Activity>
{
    private readonly WatchActivityEnricher _enricher;

    public WatchActivityProcessor(WatchActivityEnricher enricher)
    {
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
    }

    /// <summary>
    /// Called when a span starts. Enriches with Watch-specific context.
    /// </summary>
    public override void OnStart(Activity data)
    {
        _enricher.Enrich(data);
    }
}
