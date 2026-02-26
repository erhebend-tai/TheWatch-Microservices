namespace TheWatch.Shared.Notifications;

/// <summary>
/// Priority levels for push notifications.
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Channel categories for notification routing.
/// </summary>
public enum NotificationChannel
{
    Emergency,
    Dispatch,
    FamilyCheckIn,
    HealthAlert,
    DisasterAlert,
    Appointment,
    Gamification,
    System
}

/// <summary>
/// Represents a push notification message to be sent via FCM.
/// </summary>
public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.System;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ImageUrl { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];

    /// <summary>
    /// Target a single device token.
    /// </summary>
    public string? DeviceToken { get; set; }

    /// <summary>
    /// Target an FCM topic (e.g. "incident-{id}", "family-{groupId}").
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Target a condition expression (e.g. "'emergency' in topics || 'dispatch' in topics").
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// Device registration for push notifications. Stored in P1 CoreGateway.
/// </summary>
public class DeviceRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "android", "ios", "windows"
    public string? DeviceModel { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public List<string> SubscribedTopics { get; set; } = [];
}

/// <summary>
/// Result of a notification send operation.
/// </summary>
public record NotificationResult(
    bool Success,
    string? MessageId = null,
    string? Error = null);

/// <summary>
/// Cross-service notification service interface.
/// Any microservice can inject this to send push notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to a single device token.
    /// </summary>
    Task<NotificationResult> SendToDeviceAsync(
        string deviceToken, NotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// Send a notification to an FCM topic.
    /// </summary>
    Task<NotificationResult> SendToTopicAsync(
        string topic, NotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// Send a notification to multiple device tokens (batch).
    /// </summary>
    Task<List<NotificationResult>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens, NotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// Subscribe a device token to a topic.
    /// </summary>
    Task SubscribeToTopicAsync(string deviceToken, string topic, CancellationToken ct = default);

    /// <summary>
    /// Unsubscribe a device token from a topic.
    /// </summary>
    Task UnsubscribeFromTopicAsync(string deviceToken, string topic, CancellationToken ct = default);
}
