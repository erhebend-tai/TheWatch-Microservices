using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.Gamification;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddGamificationClient(this IServiceCollection services)
    {
        services.AddScoped<IGamificationClient>(sp =>
            new GamificationClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gamification")));

        return services.AddHttpClient("Gamification");
    }
}
