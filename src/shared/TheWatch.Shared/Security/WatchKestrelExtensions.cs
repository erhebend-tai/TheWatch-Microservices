using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Authentication;

namespace TheWatch.Shared.Security;

/// <summary>
/// Shared Kestrel hardening configuration applied to all TheWatch microservices.
/// Enforces request size limits, header constraints, TLS version minimums, and
/// suppresses the Server response header per DISA STIG V-222602.
/// </summary>
public static class WatchKestrelExtensions
{
    /// <summary>
    /// Configures Kestrel with production-hardened limits and TLS enforcement.
    /// Call from <c>Program.cs</c> before <c>builder.Build()</c>.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The builder for chaining.</returns>
    public static WebApplicationBuilder ConfigureWatchKestrel(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Request body: 10 MB max (prevents large payload DoS)
            options.Limits.MaxRequestBodySize = 10_485_760;

            // Request headers: 32 KB total (prevents header-based DoS)
            options.Limits.MaxRequestHeadersTotalSize = 32_768;

            // Request line (method + URI + HTTP version): 8 KB max
            options.Limits.MaxRequestLineSize = 8_192;

            // Headers timeout: 30 seconds (prevents Slowloris attacks)
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);

            // Suppress Server header to avoid version disclosure (OWASP/STIG)
            options.AddServerHeader = false;

            // Configure TLS on all HTTPS endpoints
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                // Enforce TLS 1.2 and TLS 1.3 only (FIPS 140-2 / NIST SP 800-52r2)
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });
        });

        return builder;
    }
}
