namespace TheWatch.Contracts.Wearable.Models;

public record RegisterDeviceRequest(string Name, DevicePlatform Platform, Guid OwnerId, string? Model = null, string? FirmwareVersion = null);
public record UpdateDeviceStatusRequest(DeviceStatus Status, int? BatteryPercent = null);
public record RecordHeartbeatRequest(int Bpm, int? StepCount = null, int? CaloriesBurned = null);
public record StartSyncRequest(SyncDirection Direction = SyncDirection.DeviceToServer);
