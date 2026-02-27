namespace TheWatch.Contracts.MeshNetwork.Models;

public enum NodeStatus { Online, Offline, Relaying, LowBattery }
public enum MessagePriority { Low, Normal, High, Emergency }
public enum ChannelType { Emergency, Community, DirectMessage, Broadcast }
