using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Notifications.Models;

namespace TheWatch.Contracts.Notifications;

/// <summary>
/// Typed HTTP client for TheWatch P12 Notifications service.
/// Provides push notification, broadcast, and preference management via Polly-resilient HTTP calls.
/// </summary>
public class NotificationsClient(HttpClient http) : ServiceClientBase(http, "Notifications"), INotificationsClient
{
    public Task<NotificationDto> SendAsync(SendNotificationRequest request, CancellationToken ct)
        => PostAsync<NotificationDto>("/api/notifications/send", request, ct);

    public Task<BroadcastDto> BroadcastAsync(BroadcastRequest request, CancellationToken ct)
        => PostAsync<BroadcastDto>("/api/notifications/broadcast", request, ct);

    public Task<NotificationDto?> GetAsync(Guid id, CancellationToken ct)
        => GetAsync<NotificationDto?>($"/api/notifications/record/{id}", ct);

    public Task<NotificationListResponse> ListAsync(Guid recipientId, int page, int pageSize, CancellationToken ct)
        => GetAsync<NotificationListResponse>($"/api/notifications/{recipientId}?page={page}&pageSize={pageSize}", ct);

    public Task<NotificationStats> GetStatsAsync(CancellationToken ct)
        => GetAsync<NotificationStats>("/api/notifications/stats", ct);

    public Task<NotificationPreferenceDto> SetPreferenceAsync(SetNotificationPreferenceRequest request, CancellationToken ct)
        => PostAsync<NotificationPreferenceDto>("/api/notifications/preferences", request, ct);

    public Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken ct)
        => GetAsync<List<NotificationPreferenceDto>>($"/api/notifications/preferences/{userId}", ct);
}
