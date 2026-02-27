using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.DisasterRelief;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddDisasterReliefClient(this IServiceCollection services)
    {
        services.AddScoped<IDisasterReliefClient>(sp =>
            new DisasterReliefClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("DisasterRelief")));

        return services.AddHttpClient("DisasterRelief");
    }
}
