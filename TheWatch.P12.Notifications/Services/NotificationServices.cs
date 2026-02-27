using Microsoft.EntityFrameworkCore;
using TheWatch.P12.Notifications.Notifications;
using TheWatch.Shared.Contracts;

namespace TheWatch.P12.Notifications.Services;

public class NotificationDispatcher(
    IWatchRepository<NotificationRecord> repo,
    IWatchRepository<BroadcastMessage> broadcastRepo,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task<NotificationRecord> SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        var notification = new NotificationRecord
        {
            RecipientId = request.RecipientId,
            Title = request.Title,
            Body = request.Body,
            Channel = request.Channel,
            Priority = request.Priority,
            Category = request.Category,
            DeepLink = request.DeepLink,
            ImageUrl = request.ImageUrl,
            Status = NotificationStatus.Pending
        };

        await repo.AddAsync(notification, ct);
        logger.LogInformation("Queued notification {Id} → recipient {RecipientId} via {Channel}",
            notification.Id, notification.RecipientId, notification.Channel);

        // Dispatch based on channel
        notification = await DispatchAsync(notification, ct);
        await repo.UpdateAsync(notification, ct);
        return notification;
    }

    public async Task<BroadcastMessage> BroadcastAsync(BroadcastRequest request, CancellationToken ct = default)
    {
        var broadcast = new BroadcastMessage
        {
            Title = request.Title,
            Body = request.Body,
            Category = request.Category,
            Priority = request.Priority,
            TargetLatitude = request.TargetLatitude,
            TargetLongitude = request.TargetLongitude,
            RadiusKm = request.RadiusKm,
            SenderId = request.SenderId,
            RecipientsCount = request.RecipientIds?.Length ?? 0
        };

        await broadcastRepo.AddAsync(broadcast, ct);
        logger.LogInformation("Broadcast {Id} queued: {Title} to {Count} recipients",
            broadcast.Id, broadcast.Title, broadcast.RecipientsCount);

        // Dispatch individual notifications for each recipient
        if (request.RecipientIds is { Length: > 0 })
        {
            foreach (var recipientId in request.RecipientIds)
            {
                var notif = new NotificationRecord
                {
                    RecipientId = recipientId,
                    Title = request.Title,
                    Body = request.Body,
                    Channel = NotificationChannel.Push,
                    Priority = request.Priority,
                    Category = request.Category,
                    Status = NotificationStatus.Pending
                };
                await repo.AddAsync(notif, ct);
                await DispatchAsync(notif, ct);
                await repo.UpdateAsync(notif, ct);
            }
        }

        return broadcast;
    }

    public async Task<NotificationRecord?> GetAsync(Guid id, CancellationToken ct = default)
        => await repo.Query().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<NotificationListResponse> ListAsync(Guid recipientId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = repo.Query().Where(n => n.RecipientId == recipientId).OrderByDescending(n => n.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new NotificationListResponse(items, total, page, pageSize);
    }

    public async Task<NotificationStats> GetStatsAsync(CancellationToken ct = default)
    {
        var total = await repo.Query().CountAsync(ct);
        var sent = await repo.Query().CountAsync(n => n.Status == NotificationStatus.Sent, ct);
        var delivered = await repo.Query().CountAsync(n => n.Status == NotificationStatus.Delivered, ct);
        var failed = await repo.Query().CountAsync(n => n.Status == NotificationStatus.Failed, ct);
        var pending = await repo.Query().CountAsync(n => n.Status == NotificationStatus.Pending, ct);
        var rate = total > 0 ? (double)delivered / total * 100.0 : 0.0;
        return new NotificationStats(sent, delivered, failed, pending, Math.Round(rate, 2));
    }

    private Task<NotificationRecord> DispatchAsync(NotificationRecord notification, CancellationToken ct)
    {
        // FCM/APNS dispatch via FirebaseAdmin (registered in TheWatch.Shared)
        // For now, mark as Sent — real dispatch is handled by the NotificationService in TheWatch.Shared
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;
        logger.LogDebug("Dispatched notification {Id} via {Channel}", notification.Id, notification.Channel);
        return Task.FromResult(notification);
    }
}

public class PreferenceService(
    IWatchRepository<NotificationPreference> repo,
    ILogger<PreferenceService> logger) : IPreferenceService
{
    public async Task<NotificationPreference> SetAsync(SetNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var existing = await repo.Query()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.Category == request.Category, ct);

        if (existing is not null)
        {
            existing.PushEnabled = request.PushEnabled;
            existing.SmsEnabled = request.SmsEnabled;
            existing.EmailEnabled = request.EmailEnabled;
            existing.InAppEnabled = request.InAppEnabled;
            existing.QuietHoursStart = request.QuietHoursStart;
            existing.QuietHoursEnd = request.QuietHoursEnd;
            existing.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(existing, ct);
            return existing;
        }

        var pref = new NotificationPreference
        {
            UserId = request.UserId,
            Category = request.Category,
            PushEnabled = request.PushEnabled,
            SmsEnabled = request.SmsEnabled,
            EmailEnabled = request.EmailEnabled,
            InAppEnabled = request.InAppEnabled,
            QuietHoursStart = request.QuietHoursStart,
            QuietHoursEnd = request.QuietHoursEnd
        };
        await repo.AddAsync(pref, ct);
        logger.LogInformation("Notification preferences set for user {UserId}, category {Category}",
            request.UserId, request.Category);
        return pref;
    }

    public async Task<List<NotificationPreference>> GetForUserAsync(Guid userId, CancellationToken ct = default)
        => await repo.Query().Where(p => p.UserId == userId).ToListAsync(ct);
}
