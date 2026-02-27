using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Security;

/// <summary>
/// Middleware that logs all requests with user identity, endpoint, status code, and duration.
/// Emits structured Serilog events for queryability.
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User?.FindFirstValue("sub")
                      ?? "anonymous";
            var statusCode = context.Response.StatusCode;
            var duration = sw.ElapsedMilliseconds;

            // Skip health/info endpoints to reduce noise
            if (!path.StartsWith("/health") && !path.StartsWith("/info"))
            {
                _logger.LogInformation(
                    "SecurityAudit: {Method} {Path} by {UserId} from {IP} [{UserAgent}] -> {StatusCode} in {Duration}ms",
                    method, path, userId, ip, userAgent, statusCode, duration);
            }
        }
    }
}
