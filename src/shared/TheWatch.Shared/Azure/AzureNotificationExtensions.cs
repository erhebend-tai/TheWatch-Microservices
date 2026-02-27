using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Notifications;

namespace TheWatch.Shared.Azure;

/// <summary>
/// Extensions to conditionally add Azure Communication Services notification provider.
///
/// Usage in Program.cs (after ConfigureWatchNotifications):
///   builder.ConfigureWatchNotifications();              // registers Firebase or NoOp
///   builder.Services.AddAzureNotificationsIfConfigured(builder.Configuration);  // overlays ACS if configured
///
/// When UseAzureCommunication = true, this REPLACES the INotificationService registration
/// with ACS. Firebase remains used internally for mobile push (FCM tokens), while ACS
/// handles SMS and email channels.
///
/// When UseAzureCommunication = false or not set, the existing Firebase/NoOp registration is untouched.
/// </summary>
public static class AzureNotificationExtensions
{
    public static IServiceCollection AddAzureNotificationsIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureServiceOptions.SectionName)
            .Get<AzureServiceOptions>();

        if (options is not { UseAzureCommunication: true })
            return services;

        if (string.IsNullOrWhiteSpace(options.CommunicationConnectionString))
            return services;

        // Replace the INotificationService with ACS implementation.
        // The generator-registered Firebase/NoOp service is overridden.
        services.AddSingleton<INotificationService>(sp =>
            new AzureCommunicationNotificationService(
                options.CommunicationConnectionString,
                options.CommunicationSenderEmail ?? "DoNotReply@thewatch.app",
                options.CommunicationSenderPhone ?? "+10000000000",
                sp.GetRequiredService<ILogger<AzureCommunicationNotificationService>>()));

        return services;
    }
}
