namespace TheWatch.P4.Wearable.Devices;

public enum DevicePlatform
{
    AppleWatch,
    WearOS,
    Garmin,
    Samsung,
    Fitbit,
    Other
}

public enum DeviceStatus
{
    Connected,
    Syncing,
    Disconnected,
    LowBattery,
    Error
}

public enum SyncDirection
{
    DeviceToServer,
    ServerToDevice,
    Bidirectional
}

public class WearableDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public Guid OwnerId { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Disconnected;
    public int? BatteryPercent { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool HasHeartRateSensor { get; set; } = true;
    public bool HasGpsSensor { get; set; }
    public bool HasAccelerometer { get; set; } = true;
    public bool HasGyroscope { get; set; }
    public bool HasBloodOxygen { get; set; }
    public bool HasEcg { get; set; }
    public bool HasTemperatureSensor { get; set; }
    public string? ScreenShape { get; set; }
    public int? ScreenWidthPx { get; set; }
    public int? ScreenHeightPx { get; set; }
    public int? BatteryCapacityMah { get; set; }
    public double? BatteryHealthPct { get; set; }
    public string? BluetoothVersion { get; set; }
    public bool IsDeleted { get; set; }
}

public class HeartbeatReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public int Bpm { get; set; }
    public int? StepCount { get; set; }
    public int? CaloriesBurned { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class SyncJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public SyncDirection Direction { get; set; }
    public int RecordsProcessed { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

// Request/response records

public record RegisterDeviceRequest(
    string Name,
    DevicePlatform Platform,
    Guid OwnerId,
    string? Model = null,
    string? FirmwareVersion = null);

public record UpdateDeviceStatusRequest(
    DeviceStatus Status,
    int? BatteryPercent = null);

public record RecordHeartbeatRequest(
    int Bpm,
    int? StepCount = null,
    int? CaloriesBurned = null);

public record StartSyncRequest(SyncDirection Direction = SyncDirection.DeviceToServer);

public record DeviceListResponse(
    List<WearableDevice> Items,
    int TotalCount);

public record HeartbeatHistory(
    List<HeartbeatReading> Readings,
    int TotalCount);
