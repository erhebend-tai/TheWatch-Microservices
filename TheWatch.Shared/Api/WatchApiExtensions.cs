using Microsoft.AspNetCore.Builder;

namespace TheWatch.Shared.Api;

/// <summary>
/// Convenience extension methods for TheWatch API pipeline features.
/// Aggregates ETag support, API versioning middleware, and other cross-cutting
/// HTTP-layer concerns into single-call registrations.
/// </summary>
public static class WatchApiExtensions
{
    /// <summary>
    /// Adds ETag / If-None-Match conditional response support to the pipeline.
    /// For all GET responses with a 2xx status code, a weak ETag header is computed
    /// from the response body hash. If the client sends a matching
    /// <c>If-None-Match</c> header, a 304 Not Modified is returned.
    /// </summary>
    /// <param name="app">The application builder pipeline.</param>
    /// <returns>The <paramref name="app"/> instance for chaining.</returns>
    /// <remarks>
    /// Register this middleware <b>after</b> authentication/authorization but
    /// <b>before</b> endpoint mapping so that all GET responses are covered.
    /// <code>
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.UseWatchETagSupport();
    /// // ... MapGet, MapPost, etc.
    /// </code>
    /// </remarks>
    public static IApplicationBuilder UseWatchETagSupport(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ETagMiddleware>();
    }
}
