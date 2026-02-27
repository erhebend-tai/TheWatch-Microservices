namespace TheWatch.Contracts.Notifications.Models;

public enum NotificationChannel { Push, Sms, Email, InApp, All }
public enum NotificationPriority { Low, Normal, High, Critical }
public enum NotificationStatus { Pending, Sent, Delivered, Failed, Cancelled }
public enum NotificationCategory { Emergency, Dispatch, FamilyAlert, HealthAlert, CommunityAlert, SystemAlert, Gamification, General }

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

public record NotificationDto(
    Guid Id,
    Guid RecipientId,
    string Title,
    string Body,
    NotificationChannel Channel,
    NotificationPriority Priority,
    NotificationStatus Status,
    NotificationCategory Category,
    string? DeepLink,
    DateTime? SentAt,
    DateTime? DeliveredAt,
    DateTime CreatedAt);

public record BroadcastDto(
    Guid Id,
    string Title,
    string Body,
    NotificationCategory Category,
    NotificationPriority Priority,
    int RecipientsCount,
    DateTime CreatedAt);

public record NotificationPreferenceDto(
    Guid Id,
    Guid UserId,
    NotificationCategory Category,
    bool PushEnabled,
    bool SmsEnabled,
    bool EmailEnabled,
    bool InAppEnabled,
    string? QuietHoursStart,
    string? QuietHoursEnd);

public record NotificationListResponse(
    List<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record NotificationStats(
    int TotalSent,
    int TotalDelivered,
    int TotalFailed,
    int TotalPending,
    double DeliveryRate);
