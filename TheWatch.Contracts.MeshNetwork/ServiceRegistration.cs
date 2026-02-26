using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.MeshNetwork;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddMeshNetworkClient(this IServiceCollection services)
    {
        services.AddScoped<IMeshNetworkClient>(sp =>
            new MeshNetworkClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("MeshNetwork")));

        return services.AddHttpClient("MeshNetwork");
    }
}
