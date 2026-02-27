using TheWatch.P12.Notifications.Notifications;

namespace TheWatch.P12.Notifications.Services;

public interface INotificationDispatcher
{
    Task<NotificationRecord> SendAsync(SendNotificationRequest request, CancellationToken ct = default);
    Task<BroadcastMessage> BroadcastAsync(BroadcastRequest request, CancellationToken ct = default);
    Task<NotificationRecord?> GetAsync(Guid id, CancellationToken ct = default);
    Task<NotificationListResponse> ListAsync(Guid recipientId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<NotificationStats> GetStatsAsync(CancellationToken ct = default);
}

public interface IPreferenceService
{
    Task<NotificationPreference> SetAsync(SetNotificationPreferenceRequest request, CancellationToken ct = default);
    Task<List<NotificationPreference>> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
