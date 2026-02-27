using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.VoiceEmergency;

public static class ServiceRegistration
{
    public static IHttpClientBuilder AddVoiceEmergencyClient(this IServiceCollection services)
    {
        services.AddScoped<IVoiceEmergencyClient>(sp =>
            new VoiceEmergencyClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("VoiceEmergency")));

        return services.AddHttpClient("VoiceEmergency");
    }
}
