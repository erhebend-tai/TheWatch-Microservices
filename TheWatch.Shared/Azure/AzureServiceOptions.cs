namespace TheWatch.Shared.Azure;

/// <summary>
/// Central configuration for Azure managed service toggles.
/// Bind from "Azure" section in appsettings.json.
///
/// Each toggle switches between self-hosted and Azure-managed implementation:
///   - UseAzureSignalR: self-hosted SignalR ↔ Azure SignalR Service
///   - UseAzureMaps: PostGIS ↔ Azure Maps
///   - UseAzureCommunication: Firebase ↔ Azure Communication Services
///   - UseApplicationInsights: Serilog-only ↔ Serilog + Application Insights
/// </summary>
public class AzureServiceOptions
{
    public const string SectionName = "Azure";

    /// <summary>
    /// Use Azure SignalR Service instead of self-hosted SignalR.
    /// Requires Azure:SignalR:ConnectionString to be set.
    /// </summary>
    public bool UseAzureSignalR { get; set; }

    /// <summary>
    /// Azure SignalR Service connection string.
    /// </summary>
    public string? SignalRConnectionString { get; set; }

    /// <summary>
    /// Use Azure Maps instead of self-hosted PostGIS.
    /// Requires Azure:Maps:SubscriptionKey to be set.
    /// </summary>
    public bool UseAzureMaps { get; set; }

    /// <summary>
    /// Azure Maps subscription key.
    /// </summary>
    public string? MapsSubscriptionKey { get; set; }

    /// <summary>
    /// Use Azure Communication Services instead of Firebase for SMS/email.
    /// Requires Azure:Communication:ConnectionString to be set.
    /// Firebase remains available for push notifications (FCM).
    /// </summary>
    public bool UseAzureCommunication { get; set; }

    /// <summary>
    /// Azure Communication Services connection string.
    /// </summary>
    public string? CommunicationConnectionString { get; set; }

    /// <summary>
    /// Sender email address for ACS email notifications.
    /// </summary>
    public string? CommunicationSenderEmail { get; set; }

    /// <summary>
    /// Sender phone number for ACS SMS notifications (E.164 format).
    /// </summary>
    public string? CommunicationSenderPhone { get; set; }

    /// <summary>
    /// Enable Application Insights alongside Serilog (additive, not replacement).
    /// Requires Azure:ApplicationInsights:ConnectionString to be set.
    /// </summary>
    public bool UseApplicationInsights { get; set; }

    /// <summary>
    /// Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }
}
