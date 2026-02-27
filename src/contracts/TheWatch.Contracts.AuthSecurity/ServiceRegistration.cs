using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.AuthSecurity;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddAuthSecurityClient(this IServiceCollection services)
    {
        services.AddScoped<IAuthSecurityClient>(sp =>
            new AuthSecurityClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthSecurity")));

        return services.AddHttpClient("AuthSecurity");
    }
}
