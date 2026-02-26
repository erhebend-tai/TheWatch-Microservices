namespace TheWatch.Contracts.MeshNetwork.Models;

public record RegisterNodeRequest(string Name, string? DeviceId = null, double? Latitude = null, double? Longitude = null, int? BatteryPercent = null);
public record UpdateNodeStatusRequest(NodeStatus Status, double? Latitude = null, double? Longitude = null, int? BatteryPercent = null);
public record SendMessageRequest(Guid SenderId, string Content, Guid? RecipientId = null, Guid? ChannelId = null, MessagePriority Priority = MessagePriority.Normal);
public record CreateChannelRequest(string Name, ChannelType Type);
