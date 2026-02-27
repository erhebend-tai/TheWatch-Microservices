using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Shared.Api;

/// <summary>
/// Item 229: Centralized API versioning configuration for all TheWatch microservices.
/// Uses Asp.Versioning.Http with URL segment versioning as the primary strategy
/// and header-based versioning ("X-Api-Version") as a secondary strategy.
/// </summary>
/// <remarks>
/// Usage in Program.cs:
/// <code>
/// builder.Services.AddWatchApiVersioning();
/// </code>
/// Endpoints can then be versioned via URL segments (e.g. /api/v1/profiles) or by sending
/// the <c>X-Api-Version: 1.0</c> header. When no version is specified, the default
/// version 1.0 is assumed.
/// </remarks>
public static class WatchApiVersioningExtensions
{
    /// <summary>
    /// The default API version applied when a client does not specify one.
    /// </summary>
    public static readonly ApiVersion DefaultVersion = new(1, 0);

    /// <summary>
    /// The custom header name used as the secondary version reader.
    /// </summary>
    public const string VersionHeaderName = "X-Api-Version";

    /// <summary>
    /// Registers API versioning services with TheWatch-standard configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWatchApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // When a client omits the version, default to 1.0 instead of rejecting
            options.DefaultApiVersion = DefaultVersion;
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Include api-supported-versions and api-deprecated-versions response headers
            options.ReportApiVersions = true;

            // Primary: URL segment (/api/v1/...), Secondary: custom header
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader(VersionHeaderName)
            );
        });

        return services;
    }
}
