using Microsoft.Extensions.Logging;
using TheWatch.Shared.Notifications;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Cross-platform push notification service for MAUI.
/// Handles device token registration, topic management, and notification tap routing.
/// </summary>
public class WatchPushNotificationService
{
    private readonly WatchApiClient _api;
    private readonly WatchAuthService _auth;
    private readonly ILogger<WatchPushNotificationService> _logger;
    private string? _currentToken;
    private Guid? _registrationId;

    public event Action<PushNotificationData>? OnNotificationReceived;
    public event Action<PushNotificationData>? OnNotificationTapped;

    public WatchPushNotificationService(
        WatchApiClient api,
        WatchAuthService auth,
        ILogger<WatchPushNotificationService> logger)
    {
        _api = api;
        _auth = auth;
        _logger = logger;
    }

    public string? CurrentToken => _currentToken;
    public bool IsRegistered => _registrationId.HasValue;

    /// <summary>
    /// Called by platform-specific code when a new FCM token is received.
    /// Registers or updates the device with P1 CoreGateway.
    /// </summary>
    public async Task OnTokenRefreshedAsync(string token)
    {
        _currentToken = token;
        _logger.LogInformation("FCM token refreshed: {Token}", token[..Math.Min(20, token.Length)] + "...");

        try
        {
            var userId = await _auth.GetCurrentUserIdAsync();
            if (userId == null)
            {
                _logger.LogWarning("Cannot register device — user not authenticated");
                return;
            }

            var registration = new DeviceRegistration
            {
                UserId = userId.Value,
                DeviceToken = token,
                Platform = GetPlatform(),
                DeviceModel = DeviceInfo.Current.Model,
                SubscribedTopics = GetDefaultTopics()
            };

            var result = await _api.RegisterDeviceAsync(registration);
            _registrationId = result?.Id;
            _logger.LogInformation("Device registered with P1: {RegistrationId}", _registrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device token with P1");
        }
    }

    /// <summary>
    /// Called by platform-specific code when a push notification is received (foreground).
    /// </summary>
    public void HandleNotificationReceived(PushNotificationData data)
    {
        _logger.LogInformation("Push notification received: {Title}", data.Title);
        OnNotificationReceived?.Invoke(data);
    }

    /// <summary>
    /// Called by platform-specific code when a user taps on a notification.
    /// </summary>
    public void HandleNotificationTapped(PushNotificationData data)
    {
        _logger.LogInformation("Push notification tapped: {Title} (action: {Action})", data.Title, data.Action);
        OnNotificationTapped?.Invoke(data);
    }

    /// <summary>
    /// Subscribe to an FCM topic.
    /// </summary>
    public async Task SubscribeToTopicAsync(string topic)
    {
        if (_registrationId == null || _currentToken == null) return;
        await _api.SubscribeToTopicAsync(_registrationId.Value, topic);
    }

    /// <summary>
    /// Unsubscribe from an FCM topic.
    /// </summary>
    public async Task UnsubscribeFromTopicAsync(string topic)
    {
        if (_registrationId == null || _currentToken == null) return;
        await _api.UnsubscribeFromTopicAsync(_registrationId.Value, topic);
    }

    private static string GetPlatform()
    {
#if ANDROID
        return "android";
#elif IOS
        return "ios";
#elif WINDOWS
        return "windows";
#else
        return "unknown";
#endif
    }

    private static List<string> GetDefaultTopics()
    {
        return
        [
            "watch-voiceemergency",
            "watch-familyhealth",
            "watch-disasterrelief"
        ];
    }
}

/// <summary>
/// Data extracted from a received push notification.
/// </summary>
public class PushNotificationData
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? Action { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];

    /// <summary>
    /// Determines the deep link route based on notification data.
    /// </summary>
    public string GetDeepLinkRoute()
    {
        if (Data.TryGetValue("incidentId", out var incidentId))
            return $"/sos";
        if (Data.TryGetValue("dispatchId", out var dispatchId))
            return $"/map";
        if (Data.TryGetValue("alertId", out var alertId))
            return $"/health";
        if (Data.TryGetValue("evidenceId", out var evidenceId))
            return $"/evidence";
        if (Data.TryGetValue("reportId", out var reportId) && Guid.TryParse(reportId, out var reportGuid))
            return $"/report/{reportGuid}";
        if (Data.TryGetValue("source", out var source))
        {
            return source switch
            {
                "VoiceEmergency" => "/sos",
                "FamilyHealth" => "/health",
                "DisasterRelief" => "/map",
                "FirstResponder" => "/map",
                "Gamification" => "/profile",
                "Evidence" => "/evidence",
                _ => "/"
            };
        }
        return "/";
    }
}
