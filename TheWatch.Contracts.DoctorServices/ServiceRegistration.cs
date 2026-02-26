using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.DoctorServices;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddDoctorServicesClient(this IServiceCollection services)
    {
        services.AddScoped<IDoctorServicesClient>(sp =>
            new DoctorServicesClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("DoctorServices")));

        return services.AddHttpClient("DoctorServices");
    }
}
