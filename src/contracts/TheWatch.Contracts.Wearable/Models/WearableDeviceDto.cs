namespace TheWatch.Contracts.Wearable.Models;

public class WearableDeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public Guid OwnerId { get; set; }
    public DeviceStatus Status { get; set; }
    public int? BatteryPercent { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HeartbeatReadingDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public int Bpm { get; set; }
    public int? StepCount { get; set; }
    public int? CaloriesBurned { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class SyncJobDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public SyncDirection Direction { get; set; }
    public int RecordsProcessed { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
