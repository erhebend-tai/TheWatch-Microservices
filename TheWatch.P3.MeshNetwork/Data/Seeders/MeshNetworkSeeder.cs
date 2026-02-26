using Microsoft.EntityFrameworkCore;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Data.Seeders;

public class MeshNetworkSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(MeshNetworkDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<MeshNode>().AnyAsync(ct))
            return;

        // Mesh Nodes
        var nodes = new[]
        {
            new MeshNode { Id = Guid.Parse("00000000-0000-0000-0003-000000000001"), Name = "Gateway-Alpha", DeviceId = "GW-ALPHA-001", Status = NodeStatus.Online, Latitude = 34.0522, Longitude = -118.2437, BatteryPercent = 95, RelayCount = 42 },
            new MeshNode { Id = Guid.Parse("00000000-0000-0000-0003-000000000002"), Name = "Relay-Bravo", DeviceId = "RL-BRAVO-002", Status = NodeStatus.Relaying, Latitude = 34.0530, Longitude = -118.2450, BatteryPercent = 78, RelayCount = 128 },
            new MeshNode { Id = Guid.Parse("00000000-0000-0000-0003-000000000003"), Name = "Peer-Charlie", DeviceId = "PR-CHARLIE-003", Status = NodeStatus.Online, Latitude = 34.0515, Longitude = -118.2420, BatteryPercent = 60 },
            new MeshNode { Id = Guid.Parse("00000000-0000-0000-0003-000000000004"), Name = "Peer-Delta", DeviceId = "PR-DELTA-004", Status = NodeStatus.LowBattery, Latitude = 34.0540, Longitude = -118.2460, BatteryPercent = 12 },
            new MeshNode { Id = Guid.Parse("00000000-0000-0000-0003-000000000005"), Name = "Emergency-Echo", DeviceId = "EM-ECHO-005", Status = NodeStatus.Online, Latitude = 34.0510, Longitude = -118.2430, BatteryPercent = 100, RelayCount = 5 }
        };
        context.Set<MeshNode>().AddRange(nodes);

        // Channels
        var channel1 = new NotificationChannel { Id = Guid.Parse("00000000-0000-0000-0003-000000000010"), Name = "Emergency Broadcast", Type = ChannelType.Emergency };
        var channel2 = new NotificationChannel { Id = Guid.Parse("00000000-0000-0000-0003-000000000011"), Name = "Community Updates", Type = ChannelType.Community };
        context.Set<NotificationChannel>().AddRange(channel1, channel2);

        // Messages
        var messages = new List<MeshMessage>();
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new MeshMessage
            {
                Id = Guid.Parse($"00000000-0000-0000-0003-0000000001{i:D2}"),
                SenderId = nodes[i % 5].Id,
                ChannelId = i < 3 ? channel1.Id : channel2.Id,
                RecipientId = i % 2 == 0 ? nodes[(i + 1) % 5].Id : null,
                Content = i < 3 ? $"Emergency alert #{i + 1}: Test broadcast" : $"Community message #{i + 1}: Status update",
                Priority = i < 3 ? MessagePriority.Emergency : MessagePriority.Normal,
                HopCount = i % 4,
                SentAt = DateTime.UtcNow.AddMinutes(-i * 15)
            });
        }
        context.Set<MeshMessage>().AddRange(messages);

        // Message Templates
        var templates = new[]
        {
            new MessageTemplate { Id = Guid.Parse("00000000-0000-0000-0003-000000000020"), TemplateCode = "SOS", TemplateName = "SOS Alert", TemplateCategory = "Emergency", TemplateBody = "SOS: Emergency at {location}. {details}", IsSystemTemplate = true, IsEmergencyTemplate = true, DefaultPriority = 1 },
            new MessageTemplate { Id = Guid.Parse("00000000-0000-0000-0003-000000000021"), TemplateCode = "ALL_CLEAR", TemplateName = "All Clear", TemplateCategory = "Emergency", TemplateBody = "All Clear: {situation} has been resolved.", IsSystemTemplate = true, IsEmergencyTemplate = true, DefaultPriority = 2 },
            new MessageTemplate { Id = Guid.Parse("00000000-0000-0000-0003-000000000022"), TemplateCode = "STATUS", TemplateName = "Status Update", TemplateCategory = "General", TemplateBody = "Status: {message}", IsSystemTemplate = true, DefaultPriority = 4 }
        };
        context.Set<MessageTemplate>().AddRange(templates);

        await context.SaveChangesAsync(ct);
    }
}
