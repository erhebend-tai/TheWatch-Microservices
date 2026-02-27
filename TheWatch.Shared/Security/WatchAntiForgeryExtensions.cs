using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Shared.Security;

/// <summary>
/// Configures anti-forgery (CSRF/XSRF) protection for Blazor Server applications
/// (Dashboard and Admin portal). Uses secure cookie settings aligned with OWASP
/// recommendations and DISA STIG requirements.
/// </summary>
/// <remarks>
/// <para>
/// Anti-forgery protection is critical for Blazor Server because the UI runs on
/// the server via SignalR, and form submissions must be validated against CSRF attacks.
/// This is distinct from the JWT-based API services (P1-P11) which use token-based
/// authentication and are inherently CSRF-resistant.
/// </para>
/// <para>
/// Usage in Dashboard/Admin <c>Program.cs</c>:
/// <code>
/// // Service registration:
/// builder.Services.AddWatchAntiForgery();
///
/// var app = builder.Build();
///
/// // Middleware pipeline (before routing):
/// app.UseWatchAntiForgery();
/// </code>
/// </para>
/// </remarks>
public static class WatchAntiForgeryExtensions
{
    /// <summary>
    /// Default cookie name for the anti-forgery token.
    /// Uses a <c>__Host-</c> prefix which enforces Secure, Path=/, and no Domain
    /// attributes per the Cookie Prefixes specification (draft-ietf-httpbis-rfc6265bis).
    /// </summary>
    private const string CookieName = "__Host-TheWatch.Antiforgery";

    /// <summary>
    /// Default form field name for the anti-forgery token.
    /// </summary>
    private const string FormFieldName = "__WatchRequestVerificationToken";

    /// <summary>
    /// Default header name for AJAX/fetch-based anti-forgery token submission.
    /// </summary>
    private const string HeaderName = "X-Watch-XSRF-TOKEN";

    /// <summary>
    /// Configures anti-forgery services with hardened cookie settings suitable for
    /// production deployment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchAntiForgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            // ── Cookie Settings ─────────────────────────────────────────
            // OWASP: HttpOnly prevents JavaScript access to the cookie
            options.Cookie.HttpOnly = true;

            // OWASP: Secure flag ensures cookie is only sent over HTTPS
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

            // OWASP: SameSite=Strict prevents the cookie from being sent
            // with cross-origin requests entirely (strongest CSRF protection)
            options.Cookie.SameSite = SameSiteMode.Strict;

            // Cookie name with __Host- prefix for additional security guarantees
            options.Cookie.Name = CookieName;

            // Path restricted to root — cookie applies to all paths
            options.Cookie.Path = "/";

            // Essential cookie — not subject to GDPR consent requirements
            // since it is a security mechanism, not a tracking cookie
            options.Cookie.IsEssential = true;

            // ── Token Settings ──────────────────────────────────────────
            // Form field name for server-rendered forms
            options.FormFieldName = FormFieldName;

            // Header name for AJAX/fetch requests
            options.HeaderName = HeaderName;

            // Suppress the X-Frame-Options header here — it is already set
            // by SecurityHeadersMiddleware to "DENY" for all responses
            options.SuppressXFrameOptionsHeader = true;
        });

        return services;
    }

    /// <summary>
    /// Adds anti-forgery middleware to the application pipeline. This middleware
    /// validates anti-forgery tokens on state-changing requests (POST, PUT, DELETE)
    /// and generates new tokens for GET requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be called in the middleware pipeline <b>after</b> authentication
    /// and <b>before</b> routing/endpoints. For Blazor Server applications, the
    /// built-in <c>app.UseAntiforgery()</c> handles integration with Razor
    /// components automatically.
    /// </para>
    /// <para>
    /// The middleware validates the token on all POST/PUT/DELETE/PATCH requests
    /// unless the endpoint has the <c>[IgnoreAntiforgeryToken]</c> attribute.
    /// API endpoints using JWT Bearer authentication should use
    /// <c>[IgnoreAntiforgeryToken]</c> since they are CSRF-resistant by design.
    /// </para>
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseWatchAntiForgery(this IApplicationBuilder app)
    {
        // Use the built-in ASP.NET Core antiforgery middleware
        // For Blazor Server, this integrates with EditForm and form handling
        app.UseAntiforgery();

        return app;
    }
}
