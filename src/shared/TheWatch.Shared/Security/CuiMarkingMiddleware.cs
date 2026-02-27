using Microsoft.AspNetCore.Http;

namespace TheWatch.Shared.Security;

/// <summary>
/// Adds Controlled Unclassified Information (CUI) marking headers to API responses
/// for authenticated requests. Implements NIST SP 800-171 CUI marking requirements
/// by categorizing API routes into their appropriate CUI privacy categories.
/// </summary>
/// <remarks>
/// CUI categories applied:
/// <list type="bullet">
///   <item><c>/api/auth/*</c> — CUI//SP-PRIV (Privacy)</item>
///   <item><c>/api/*/vital*</c>, <c>/api/*/medical*</c>, <c>/api/*/health*</c> — CUI//SP-HLTH (Health)</item>
///   <item><c>/api/*/incident*</c>, <c>/api/*/evidence*</c>, <c>/api/*/surveillance*</c> — CUI//SP-LEI (Law Enforcement)</item>
///   <item><c>/api/*/location*</c>, <c>/api/*/geo*</c> — CUI//SP-GEO (Geolocation)</item>
///   <item>All other authenticated routes — CUI//SP-BASIC</item>
/// </list>
/// Register after authentication/authorization middleware:
/// <code>app.UseMiddleware&lt;CuiMarkingMiddleware&gt;();</code>
/// </remarks>
public class CuiMarkingMiddleware(RequestDelegate next)
{
    private const string CuiBanner = "CONTROLLED // UNCLASSIFIED INFORMATION";
    private const string CuiHeaderCategory = "X-CUI-Category";
    private const string CuiHeaderBanner = "X-CUI-Banner";

    /// <summary>
    /// Inspects the request path and user authentication state, then appends
    /// appropriate CUI marking headers before the response is sent.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply CUI markings to authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.OnStarting(() =>
            {
                var category = ClassifyRoute(context.Request.Path);
                context.Response.Headers[CuiHeaderCategory] = category;
                context.Response.Headers[CuiHeaderBanner] = CuiBanner;
                return Task.CompletedTask;
            });
        }

        await next(context);
    }

    /// <summary>
    /// Classifies an API route path into its CUI privacy category based on
    /// path segment matching.
    /// </summary>
    /// <param name="path">The request path to classify.</param>
    /// <returns>The CUI category string (e.g., "CUI//SP-HLTH").</returns>
    internal static string ClassifyRoute(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Privacy: authentication and authorization endpoints
        if (pathValue.StartsWith("/api/auth/", StringComparison.Ordinal) ||
            pathValue.Equals("/api/auth", StringComparison.Ordinal))
        {
            return "CUI//SP-PRIV";
        }

        // Health information: vitals, medical records, health data
        if (ContainsSegmentPrefix(pathValue, "vital") ||
            ContainsSegmentPrefix(pathValue, "medical") ||
            ContainsSegmentPrefix(pathValue, "health"))
        {
            return "CUI//SP-HLTH";
        }

        // Law enforcement: incidents, evidence, surveillance
        if (ContainsSegmentPrefix(pathValue, "incident") ||
            ContainsSegmentPrefix(pathValue, "evidence") ||
            ContainsSegmentPrefix(pathValue, "surveillance"))
        {
            return "CUI//SP-LEI";
        }

        // Geolocation: location tracking, geospatial data
        if (ContainsSegmentPrefix(pathValue, "location") ||
            ContainsSegmentPrefix(pathValue, "geo"))
        {
            return "CUI//SP-GEO";
        }

        return "CUI//SP-BASIC";
    }

    /// <summary>
    /// Checks if any path segment (after splitting on '/') starts with the given prefix.
    /// This matches route patterns like <c>/api/v1/vital-signs</c> or <c>/api/incidents/123</c>.
    /// </summary>
    private static bool ContainsSegmentPrefix(string pathValue, string prefix)
    {
        // Split path into segments and check if any starts with the prefix
        var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
