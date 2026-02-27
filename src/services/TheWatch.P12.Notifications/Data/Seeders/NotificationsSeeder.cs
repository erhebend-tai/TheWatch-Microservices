using Microsoft.EntityFrameworkCore;
using TheWatch.P12.Notifications.Notifications;
using TheWatch.Shared.Contracts;

namespace TheWatch.P12.Notifications.Data.Seeders;

public class NotificationsSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(NotificationsDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<NotificationRecord>().AnyAsync(ct))
            return;

        var userId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var userId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");

        var notifications = new[]
        {
            new NotificationRecord
            {
                Id = Guid.Parse("00000000-0000-0000-0012-000000000001"),
                RecipientId = userId1,
                Title = "SOS Activated",
                Body = "Your emergency SOS has been received. Responders are on the way.",
                Channel = NotificationChannel.Push,
                Priority = NotificationPriority.Critical,
                Category = NotificationCategory.Emergency,
                Status = NotificationStatus.Delivered,
                SentAt = DateTime.UtcNow.AddMinutes(-5),
                DeliveredAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new NotificationRecord
            {
                Id = Guid.Parse("00000000-0000-0000-0012-000000000002"),
                RecipientId = userId2,
                Title = "Family Check-In Alert",
                Body = "Your family member has not checked in. Last location: Downtown LA.",
                Channel = NotificationChannel.Push,
                Priority = NotificationPriority.High,
                Category = NotificationCategory.FamilyAlert,
                Status = NotificationStatus.Delivered,
                SentAt = DateTime.UtcNow.AddMinutes(-30),
                DeliveredAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new NotificationRecord
            {
                Id = Guid.Parse("00000000-0000-0000-0012-000000000003"),
                RecipientId = userId1,
                Title = "Community Alert",
                Body = "Suspicious activity reported near your registered address.",
                Channel = NotificationChannel.Push,
                Priority = NotificationPriority.Normal,
                Category = NotificationCategory.CommunityAlert,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        context.Set<NotificationRecord>().AddRange(notifications);
        await context.SaveChangesAsync(ct);
    }
}
