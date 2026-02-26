using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Azure;

namespace TheWatch.Geospatial.Services;

/// <summary>
/// Extensions to register the correct IGeospatialService implementation
/// based on configuration.
///
/// Azure:UseAzureMaps = false (default): PostGisGeospatialService (requires PostgreSQL + PostGIS)
/// Azure:UseAzureMaps = true: AzureMapsGeospatialService (requires Azure Maps subscription key)
/// </summary>
public static class GeospatialProviderExtensions
{
    public static IServiceCollection AddGeospatialProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureServiceOptions.SectionName)
            .Get<AzureServiceOptions>();

        if (options is { UseAzureMaps: true } &&
            !string.IsNullOrWhiteSpace(options.MapsSubscriptionKey))
        {
            services.AddHttpClient<AzureMapsGeospatialService>();
            services.AddScoped<IGeospatialService>(sp =>
                new AzureMapsGeospatialService(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AzureMapsGeospatialService)),
                    options.MapsSubscriptionKey,
                    sp.GetRequiredService<ILogger<AzureMapsGeospatialService>>()));
        }
        else
        {
            // Default: PostGIS implementation (existing behavior)
            services.AddScoped<IGeospatialService, PostGisGeospatialService>();
        }

        return services;
    }
}
