using System.Text.RegularExpressions;

namespace TheWatch.Admin.RestAPI.Middleware;

/// <summary>
/// Propagates X-Correlation-Id across all service calls for distributed tracing.
/// If no correlation ID is present, generates a new one.
/// Security+ Domain 1.2 (Zero Trust) — sanitize client-supplied correlation IDs to prevent log injection.
/// </summary>
public partial class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();

        // Sanitize: only allow alphanumeric + hyphens, max 64 chars (prevent log injection / header injection)
        var correlationId = !string.IsNullOrEmpty(incoming) && SafeCorrelationIdPattern().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }

    // Only alphanumeric, hyphens, 1-64 chars
    [GeneratedRegex(@"^[a-zA-Z0-9\-]{1,64}$")]
    private static partial Regex SafeCorrelationIdPattern();
}
