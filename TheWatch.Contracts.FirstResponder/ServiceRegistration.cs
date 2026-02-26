using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.FirstResponder;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddFirstResponderClient(this IServiceCollection services)
    {
        services.AddScoped<IFirstResponderClient>(sp =>
            new FirstResponderClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("FirstResponder")));

        return services.AddHttpClient("FirstResponder");
    }
}
