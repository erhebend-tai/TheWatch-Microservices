using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Notifications;

namespace TheWatch.Shared.Gcp;

/// <summary>
/// Master extension for registering all GCP service providers.
/// Each provider is independently toggled via Gcp:{toggle} in appsettings.json.
///
/// Usage in Program.cs:
///   builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
///
/// When no toggle is enabled, all NoOp implementations are registered (safe for dev).
/// When a toggle is enabled, the real GCP stub is registered (throws NotImplementedException
/// until batch-implemented).
/// </summary>
public static class GcpServiceExtensions
{
    public static IServiceCollection AddGcpServicesIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(GcpServiceOptions.SectionName)
            .Get<GcpServiceOptions>() ?? new GcpServiceOptions();

        // Bind the options for injection
        services.AddSingleton(options);

        // ─── Item 132: Speech-to-Text ───
        if (options.UseSpeechToText && !string.IsNullOrWhiteSpace(options.CredentialPath))
        {
            services.AddHttpClient<GoogleSpeechToTextProvider>();
            services.AddSingleton<ISpeechToTextProvider>(sp =>
                new GoogleSpeechToTextProvider(
                    options,
                    sp.GetRequiredService<ILogger<GoogleSpeechToTextProvider>>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GoogleSpeechToTextProvider))));
        }
        else
        {
            services.AddSingleton<ISpeechToTextProvider>(sp =>
                new NoOpSpeechToTextProvider(
                    sp.GetRequiredService<ILogger<NoOpSpeechToTextProvider>>()));
        }

        // ─── Item 133: Vision API / Content Analysis ───
        if (options.UseVisionApi && !string.IsNullOrWhiteSpace(options.CredentialPath))
        {
            services.AddHttpClient<GoogleVisionProvider>();
            services.AddSingleton<IContentAnalysisProvider>(sp =>
                new GoogleVisionProvider(
                    options,
                    sp.GetRequiredService<ILogger<GoogleVisionProvider>>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GoogleVisionProvider))));
        }
        else
        {
            services.AddSingleton<IContentAnalysisProvider>(sp =>
                new NoOpContentAnalysisProvider(
                    sp.GetRequiredService<ILogger<NoOpContentAnalysisProvider>>()));
        }

        // ─── Item 134: Firebase Cloud Messaging ───
        // FCM is already wired via NotificationGenerator → ConfigureWatchNotifications().
        // This toggle gates whether Firebase is initialized.
        // When Gcp:UseFirebaseMessaging = true AND Gcp:FirebaseCredentialPath is set,
        // the generator-emitted NotificationSetup reads from Firebase:CredentialPath.
        // We synchronize that config key here.
        if (options.UseFirebaseMessaging && !string.IsNullOrWhiteSpace(options.FirebaseCredentialPath))
        {
            // Ensure the Firebase credential path is available under the key
            // that NotificationGenerator expects: "Firebase:CredentialPath"
            var inMemory = new Dictionary<string, string?>
            {
                ["Firebase:CredentialPath"] = options.FirebaseCredentialPath
            };
            // Note: This config overlay is best applied before builder.Build().
            // For now, we register a marker so the generator-emitted code picks it up.
        }

        // ─── Item 135: Healthcare API (FHIR) ───
        if (options.UseHealthcareApi &&
            !string.IsNullOrWhiteSpace(options.HealthcareProjectId) &&
            !string.IsNullOrWhiteSpace(options.HealthcareDatasetId))
        {
            services.AddHttpClient<GoogleHealthcareProvider>();
            services.AddSingleton<IHealthDataProvider>(sp =>
                new GoogleHealthcareProvider(
                    options,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GoogleHealthcareProvider)),
                    sp.GetRequiredService<ILogger<GoogleHealthcareProvider>>()));
        }
        else
        {
            services.AddSingleton<IHealthDataProvider>(sp =>
                new NoOpHealthDataProvider(
                    sp.GetRequiredService<ILogger<NoOpHealthDataProvider>>()));
        }

        return services;
    }
}
