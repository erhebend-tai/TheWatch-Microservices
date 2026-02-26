namespace TheWatch.P3.MeshNetwork.Mesh;

public enum NodeStatus
{
    Online,
    Offline,
    Relaying,
    LowBattery
}

public enum MessagePriority
{
    Low,
    Normal,
    High,
    Emergency
}

public enum ChannelType
{
    Emergency,
    Community,
    DirectMessage,
    Broadcast
}

public class MeshNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public NodeStatus Status { get; set; } = NodeStatus.Offline;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? BatteryPercent { get; set; }
    public int RelayCount { get; set; }
    public List<Guid> ConnectedPeers { get; set; } = [];
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MeshMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid? RecipientId { get; set; }
    public Guid? ChannelId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;
    public int HopCount { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class NotificationChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public List<Guid> SubscriberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Request/response records

public record RegisterNodeRequest(
    string Name,
    string? DeviceId = null,
    double? Latitude = null,
    double? Longitude = null,
    int? BatteryPercent = null);

public record UpdateNodeStatusRequest(
    NodeStatus Status,
    double? Latitude = null,
    double? Longitude = null,
    int? BatteryPercent = null);

public record SendMessageRequest(
    Guid SenderId,
    string Content,
    Guid? RecipientId = null,
    Guid? ChannelId = null,
    MessagePriority Priority = MessagePriority.Normal);

public record CreateChannelRequest(
    string Name,
    ChannelType Type);

public record NodeListResponse(
    List<MeshNode> Items,
    int TotalCount);

public record TopologyResponse(
    List<MeshNode> Nodes,
    List<PeerConnection> Connections);

public record PeerConnection(Guid NodeA, Guid NodeB);
