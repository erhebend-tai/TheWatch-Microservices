using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Shared.Security;

/// <summary>
/// Extension methods that register TheWatch production middleware into the ASP.NET Core
/// pipeline. These encapsulate the correct ordering and configuration so that each
/// microservice <c>Program.cs</c> can add hardened middleware with a single call.
/// </summary>
/// <remarks>
/// Recommended registration order in <c>Program.cs</c>:
/// <code>
/// var app = builder.Build();
///
/// app.UseWatchExceptionHandler();      // Must be first — catches all downstream exceptions
/// app.UseWatchPiiRedaction();           // Before request logging — scrubs bodies before they are logged
/// app.UseWatchResponseCompression();   // Before routing — compresses all responses
///
/// app.UseCors();
/// app.UseAuthentication();
/// app.UseAuthorization();
/// // ... remaining middleware and endpoints
/// </code>
/// </remarks>
public static class ProductionMiddlewareExtensions
{
    // ------------------------------------------------------------------
    // Global Exception Handler
    // ------------------------------------------------------------------

    /// <summary>
    /// Adds the <see cref="GlobalExceptionHandlerMiddleware"/> to the pipeline. This must
    /// be registered <b>before</b> all other middleware so that any unhandled exception from
    /// any component is caught and converted to an RFC 9457 Problem Details response.
    /// </summary>
    /// <param name="app">The web application builder pipeline.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseWatchExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }

    // ------------------------------------------------------------------
    // PII Redaction
    // ------------------------------------------------------------------

    /// <summary>
    /// Adds the <see cref="PiiRedactionMiddleware"/> to the pipeline. Register this
    /// <b>before</b> Serilog request-logging middleware so that logged request and response
    /// bodies have PII removed before they are persisted.
    /// </summary>
    /// <param name="app">The web application builder pipeline.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseWatchPiiRedaction(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PiiRedactionMiddleware>();
    }

    // ------------------------------------------------------------------
    // Response Compression (Brotli + gzip)
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers response compression services on the <see cref="IServiceCollection"/>
    /// with Brotli (preferred) and gzip as a fallback. Call this during service registration
    /// (<c>builder.Services</c>) before <c>builder.Build()</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Compression is configured with the following policies:
    /// <list type="bullet">
    ///   <item>Brotli is the preferred algorithm and is configured at
    ///     <see cref="CompressionLevel.Optimal"/> for best compression ratio.</item>
    ///   <item>gzip is the fallback for clients that do not support Brotli and is configured
    ///     at <see cref="CompressionLevel.Fastest"/> for low latency.</item>
    ///   <item>HTTPS responses are compressed (opted in explicitly since ASP.NET Core
    ///     disables this by default as a BREACH mitigation). TheWatch APIs do not reflect
    ///     user-controlled secrets in response bodies, so BREACH is not applicable.</item>
    ///   <item>Additional MIME types beyond the defaults are included:
    ///     <c>application/problem+json</c>, <c>application/grpc</c>, and
    ///     <c>application/wasm</c>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;

            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // Include additional MIME types commonly used by TheWatch services.
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/problem+json",
                "application/grpc",
                "application/wasm",
                "image/svg+xml",
                "application/font-woff2"
            });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }

    /// <summary>
    /// Adds response compression middleware to the pipeline. Call this <b>before</b> routing
    /// and static-file middleware so that responses are compressed transparently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must also call <see cref="AddWatchResponseCompression"/> during service
    /// registration for this middleware to function. Example:
    /// </para>
    /// <code>
    /// // In service registration:
    /// builder.Services.AddWatchResponseCompression();
    ///
    /// var app = builder.Build();
    ///
    /// // In middleware pipeline:
    /// app.UseWatchResponseCompression();
    /// </code>
    /// </remarks>
    /// <param name="app">The web application builder pipeline.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UseWatchResponseCompression(this IApplicationBuilder app)
    {
        return app.UseResponseCompression();
    }
}
