namespace TheWatch.Contracts.MeshNetwork.Models;

public class MeshNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public NodeStatus Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? BatteryPercent { get; set; }
    public int RelayCount { get; set; }
    public List<Guid> ConnectedPeers { get; set; } = [];
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeshMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid? RecipientId { get; set; }
    public Guid? ChannelId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessagePriority Priority { get; set; }
    public int HopCount { get; set; }
    public DateTime SentAt { get; set; }
}

public class NotificationChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public List<Guid> SubscriberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
