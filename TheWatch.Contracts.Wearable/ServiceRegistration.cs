using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.Wearable;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddWearableClient(this IServiceCollection services)
    {
        services.AddScoped<IWearableClient>(sp =>
            new WearableClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Wearable")));

        return services.AddHttpClient("Wearable");
    }
}
