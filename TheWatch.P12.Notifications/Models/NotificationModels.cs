namespace TheWatch.P12.Notifications.Notifications;

public enum NotificationChannel
{
    Push,
    Sms,
    Email,
    InApp,
    All
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Cancelled
}

public enum NotificationCategory
{
    Emergency,
    Dispatch,
    FamilyAlert,
    HealthAlert,
    CommunityAlert,
    SystemAlert,
    Gamification,
    General
}

public class NotificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Push;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public NotificationCategory Category { get; set; } = NotificationCategory.General;
    public string? DeepLink { get; set; }
    public string? ImageUrl { get; set; }
    public string? FcmMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public NotificationCategory Category { get; set; }
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BroadcastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; } = NotificationCategory.CommunityAlert;
    public NotificationPriority Priority { get; set; } = NotificationPriority.High;
    public string? TargetArea { get; set; }
    public double? TargetLatitude { get; set; }
    public double? TargetLongitude { get; set; }
    public double? RadiusKm { get; set; }
    public Guid SenderId { get; set; }
    public int RecipientsCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Request/response DTOs

public record SendNotificationRequest(
    Guid RecipientId,
    string Title,
    string Body,
    NotificationChannel Channel = NotificationChannel.Push,
    NotificationPriority Priority = NotificationPriority.Normal,
    NotificationCategory Category = NotificationCategory.General,
    string? DeepLink = null,
    string? ImageUrl = null);

public record BroadcastRequest(
    string Title,
    string Body,
    NotificationCategory Category,
    NotificationPriority Priority = NotificationPriority.High,
    Guid[]? RecipientIds = null,
    double? TargetLatitude = null,
    double? TargetLongitude = null,
    double? RadiusKm = null,
    Guid SenderId = default);

public record SetNotificationPreferenceRequest(
    Guid UserId,
    NotificationCategory Category,
    bool PushEnabled = true,
    bool SmsEnabled = false,
    bool EmailEnabled = true,
    bool InAppEnabled = true,
    string? QuietHoursStart = null,
    string? QuietHoursEnd = null);

public record NotificationListResponse(
    List<NotificationRecord> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record NotificationStats(
    int TotalSent,
    int TotalDelivered,
    int TotalFailed,
    int TotalPending,
    double DeliveryRate);
