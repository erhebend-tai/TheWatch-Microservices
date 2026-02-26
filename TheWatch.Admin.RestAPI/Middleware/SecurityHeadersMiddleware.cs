namespace TheWatch.Admin.RestAPI.Middleware;

/// <summary>
/// OWASP Secure Headers Project compliance.
/// Security+ Domain 2.1 (Attack Surface Reduction), Domain 3.1 (Defense in Depth).
/// Applies hardened response headers to every response.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing (XSS via content-type confusion)
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking — gateway API should never be framed
        headers["X-Frame-Options"] = "DENY";

        // Content Security Policy — API-only, no inline content
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Control referrer leakage
        headers["Referrer-Policy"] = "no-referrer";

        // Disable browser features not needed by an API
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Prevent caching of authenticated API responses (Security+ 5.1 — data at rest)
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
        headers["Pragma"] = "no-cache";

        // Remove server identity disclosure
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        // Cross-Origin policies — strict isolation for API
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Cross-Origin-Embedder-Policy"] = "require-corp";

        await next(context);
    }
}
