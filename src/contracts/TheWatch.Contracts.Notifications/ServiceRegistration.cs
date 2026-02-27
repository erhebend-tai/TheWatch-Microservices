using Microsoft.Extensions.DependencyInjection;

namespace TheWatch.Contracts.Notifications;

/// <summary>
/// DI registration for the TheWatch Notifications typed client.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers <see cref="INotificationsClient"/> and its underlying named HttpClient.
    /// Chain with <c>.AddWatchClientDefaults(baseAddress)</c> to apply Polly resilience policies.
    /// </summary>
    public static IHttpClientBuilder AddNotificationsClient(this IServiceCollection services)
    {
        services.AddScoped<INotificationsClient>(sp =>
            new NotificationsClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Notifications")));

        return services.AddHttpClient("Notifications");
    }
}
