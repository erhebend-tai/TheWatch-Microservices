using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.CoreGateway;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddCoreGatewayClient(this IServiceCollection services)
    {
        services.AddScoped<ICoreGatewayClient>(sp =>
            new CoreGatewayClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("CoreGateway")));

        return services.AddHttpClient("CoreGateway");
    }
}
