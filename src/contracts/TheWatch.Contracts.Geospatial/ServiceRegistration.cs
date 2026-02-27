using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.Geospatial;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddGeospatialClient(this IServiceCollection services)
    {
        services.AddScoped<IGeospatialClient>(sp =>
            new GeospatialClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Geospatial")));

        return services.AddHttpClient("Geospatial");
    }
}
