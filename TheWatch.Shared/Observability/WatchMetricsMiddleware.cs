using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TheWatch.Shared.Observability;

/// <summary>
/// Item 244: ASP.NET Core middleware that automatically records Prometheus request
/// metrics for every HTTP request. Captures method, route template, status code,
/// and request duration in seconds.
/// </summary>
/// <remarks>
/// This middleware should be registered early in the pipeline (after exception
/// handling and CORS, but before authentication) so that the recorded duration
/// includes auth and business logic time. Example:
/// <code>
/// app.UseWatchMetrics();    // registers this middleware
/// app.UseAuthentication();
/// app.UseAuthorization();
/// </code>
/// </remarks>
public class WatchMetricsMiddleware(RequestDelegate next, IWatchPrometheusMetrics metrics)
{
    /// <summary>
    /// The service name is resolved once from the entry assembly and cached.
    /// </summary>
    private static readonly string ServiceName =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var route = ResolveRoute(context);
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var durationSeconds = stopwatch.Elapsed.TotalSeconds;

            metrics.RecordRequestDuration(ServiceName, method, route, statusCode, durationSeconds);

            // Record auth failures (401/403) for the auth failure counter
            if (statusCode is 401 or 403)
            {
                var reason = statusCode == 401 ? "Unauthorized" : "Forbidden";
                metrics.RecordAuthFailure(ServiceName, reason, method);
            }
        }
    }

    /// <summary>
    /// Resolves the route template for the matched endpoint, falling back to the
    /// raw request path if no endpoint was matched (e.g. 404 responses).
    /// Using the route template rather than the raw path prevents high-cardinality
    /// label explosion in Prometheus.
    /// </summary>
    private static string ResolveRoute(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";
        }

        // Fallback: normalize the path to reduce cardinality
        // Replace GUIDs and numeric IDs with placeholders
        var path = context.Request.Path.Value ?? "/";
        return NormalizePath(path);
    }

    /// <summary>
    /// Normalizes a request path by replacing GUID segments and numeric-only segments
    /// with <c>{id}</c> placeholders to prevent Prometheus label cardinality explosion.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return "/";

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (Guid.TryParse(segments[i], out _) || IsNumeric(segments[i]))
            {
                segments[i] = "{id}";
            }
        }

        return "/" + string.Join("/", segments);
    }

    private static bool IsNumeric(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty) return false;
        foreach (var c in value)
        {
            if (!char.IsDigit(c)) return false;
        }
        return true;
    }
}
