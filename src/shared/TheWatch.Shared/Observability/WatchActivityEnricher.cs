using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace TheWatch.Shared.Observability;

/// <summary>
/// Enriches OpenTelemetry Activity spans with TheWatch-specific context from HTTP
/// request headers. When services propagate correlation, user, device, and incident
/// identifiers via headers, this enricher captures them as span tags for distributed
/// trace correlation in Jaeger, Zipkin, Grafana Tempo, or Application Insights.
/// </summary>
/// <remarks>
/// <para>
/// The following request headers are captured as Activity tags:
/// <list type="table">
///   <listheader><term>Header</term><description>Tag Name</description></listheader>
///   <item><term>X-Correlation-Id</term><description>watch.correlation_id</description></item>
///   <item><term>X-User-Id</term><description>watch.user_id</description></item>
///   <item><term>X-Device-Id</term><description>watch.device_id</description></item>
///   <item><term>X-Incident-Id</term><description>watch.incident_id</description></item>
/// </list>
/// </para>
/// <para>
/// Additionally, <c>service.name</c> and <c>service.version</c> tags are set from
/// the configured service name and assembly version, providing full service identity
/// on every span.
/// </para>
/// <para>
/// Register via <see cref="WatchTracingExtensions.AddWatchTracing"/>:
/// <code>
/// builder.Services.AddWatchTracing("TheWatch.P2.VoiceEmergency");
/// </code>
/// </para>
/// </remarks>
public sealed class WatchActivityEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _serviceName;
    private readonly string _serviceVersion;

    /// <summary>Header names read from incoming HTTP requests.</summary>
    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-Id";
        public const string UserId = "X-User-Id";
        public const string DeviceId = "X-Device-Id";
        public const string IncidentId = "X-Incident-Id";
    }

    /// <summary>Activity tag names set on spans.</summary>
    public static class Tags
    {
        public const string CorrelationId = "watch.correlation_id";
        public const string UserId = "watch.user_id";
        public const string DeviceId = "watch.device_id";
        public const string IncidentId = "watch.incident_id";
        public const string ServiceName = "service.name";
        public const string ServiceVersion = "service.version";
    }

    public WatchActivityEnricher(
        IHttpContextAccessor httpContextAccessor,
        string serviceName,
        string? serviceVersion = null)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _serviceVersion = serviceVersion
            ?? typeof(WatchActivityEnricher).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    /// <summary>
    /// Enriches the current <see cref="Activity"/> with Watch-specific tags
    /// extracted from the current HTTP request headers.
    /// </summary>
    public void Enrich(Activity activity)
    {
        if (activity is null) return;

        // Always set service identity tags
        activity.SetTag(Tags.ServiceName, _serviceName);
        activity.SetTag(Tags.ServiceVersion, _serviceVersion);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        EnrichFromHeader(activity, httpContext, Headers.CorrelationId, Tags.CorrelationId);
        EnrichFromHeader(activity, httpContext, Headers.UserId, Tags.UserId);
        EnrichFromHeader(activity, httpContext, Headers.DeviceId, Tags.DeviceId);
        EnrichFromHeader(activity, httpContext, Headers.IncidentId, Tags.IncidentId);
    }

    private static void EnrichFromHeader(
        Activity activity,
        HttpContext httpContext,
        string headerName,
        string tagName)
    {
        if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                activity.SetTag(tagName, value);
            }
        }
    }
}
