using Microsoft.AspNetCore.Http;

namespace TheWatch.Shared.Security;

/// <summary>
/// Security+ 2.2: OWASP Secure Headers Project compliance.
/// Applies hardened response headers to every response across all services.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing (XSS via content-type confusion)
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Enforce HTTPS via HSTS (max-age 2 years, include subdomains, allow preload list)
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";

        // Content Security Policy — API-only, no inline content
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Control referrer leakage
        headers["Referrer-Policy"] = "no-referrer";

        // Disable browser features not needed by an API
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Prevent caching of authenticated API responses
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        headers["Pragma"] = "no-cache";

        // Remove server identity disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        // Cross-Origin policies — strict isolation
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Cross-Origin-Embedder-Policy"] = "require-corp";

        await next(context);
    }
}
