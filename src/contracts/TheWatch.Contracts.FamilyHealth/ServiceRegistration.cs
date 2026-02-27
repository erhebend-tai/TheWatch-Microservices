using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.FamilyHealth;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddFamilyHealthClient(this IServiceCollection services)
    {
        services.AddScoped<IFamilyHealthClient>(sp =>
            new FamilyHealthClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("FamilyHealth")));

        return services.AddHttpClient("FamilyHealth");
    }
}
