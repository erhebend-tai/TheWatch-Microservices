using TheWatch.Contracts.Notifications.Models;

namespace TheWatch.Contracts.Notifications;

/// <summary>
/// Typed client interface for TheWatch P12 Notifications service.
/// </summary>
public interface INotificationsClient
{
    Task<NotificationDto> SendAsync(SendNotificationRequest request, CancellationToken ct = default);
    Task<BroadcastDto> BroadcastAsync(BroadcastRequest request, CancellationToken ct = default);
    Task<NotificationDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<NotificationListResponse> ListAsync(Guid recipientId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<NotificationStats> GetStatsAsync(CancellationToken ct = default);
    Task<NotificationPreferenceDto> SetPreferenceAsync(SetNotificationPreferenceRequest request, CancellationToken ct = default);
    Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken ct = default);
}
