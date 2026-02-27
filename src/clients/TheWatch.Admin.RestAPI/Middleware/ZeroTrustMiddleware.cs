using System.Security.Claims;

namespace TheWatch.Admin.RestAPI.Middleware;

/// <summary>
/// Zero Trust defense-in-depth — Security+ Domain 1.2 (Zero Trust), Domain 4.1 (Logging & Monitoring).
/// Validates token expiry, subject claim, issuer, and logs every request with SIEM-ready event categories.
/// Runs AFTER ASP.NET authentication — provides additional validation beyond what JWT middleware does.
/// </summary>
public class ZeroTrustMiddleware(RequestDelegate next, ILogger<ZeroTrustMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid as string : "none";

        // Skip validation for unauthenticated endpoints (health, OpenAPI)
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            logger.LogDebug("[ZT:UNAUTH] {Method} {Path} IP={IP} UA={UserAgent} [{CorrelationId}]",
                method, path, ip, userAgent, correlationId);
            await next(context);
            return;
        }

        // Defense-in-depth: validate token expiry (JWT middleware already checks, but we double-check)
        var expClaim = user.FindFirst("exp");
        if (expClaim is not null && long.TryParse(expClaim.Value, out var expUnix))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            if (expiry < DateTimeOffset.UtcNow)
            {
                logger.LogWarning("[SEC:EXPIRED_TOKEN] {Method} {Path} IP={IP} Sub={Sub} ExpiredAt={Expiry} [{CorrelationId}]",
                    method, path, ip, user.FindFirst(ClaimTypes.NameIdentifier)?.Value, expiry, correlationId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        // Validate subject claim exists (Security+ 1.3 — AAA: Authentication)
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subject))
        {
            logger.LogWarning("[SEC:MISSING_SUBJECT] {Method} {Path} IP={IP} [{CorrelationId}]",
                method, path, ip, correlationId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate issuer matches expected (defense-in-depth against token substitution)
        var issuer = user.FindFirst("iss")?.Value;
        if (issuer is not null && issuer != "TheWatch.P5.AuthSecurity")
        {
            logger.LogWarning("[SEC:INVALID_ISSUER] {Method} {Path} IP={IP} Sub={Sub} Issuer={Issuer} [{CorrelationId}]",
                method, path, ip, subject, issuer, correlationId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Security+ 4.1 — Audit log every authenticated request (SIEM-ready structured fields)
        var roles = string.Join(",", user.FindAll(ClaimTypes.Role).Select(c => c.Value));
        logger.LogInformation("[ZT:ACCESS] {Method} {Path} | Sub={Subject} Roles=[{Roles}] IP={IP} [{CorrelationId}]",
            method, path, subject, roles, ip, correlationId);

        // Detect privilege escalation attempts (non-admin hitting admin-only endpoints)
        if (path.StartsWith("/api/admin/") && !path.StartsWith("/api/admin/auth/") && !roles.Contains("Admin"))
        {
            logger.LogWarning("[SEC:PRIVILEGE_ESCALATION] {Method} {Path} | Sub={Subject} Roles=[{Roles}] IP={IP} [{CorrelationId}]",
                method, path, subject, roles, ip, correlationId);
            // Don't block here — let [Authorize] policy handle it. This is for alerting only.
        }

        await next(context);
    }
}
