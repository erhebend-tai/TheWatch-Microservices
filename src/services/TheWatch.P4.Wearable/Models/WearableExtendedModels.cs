namespace TheWatch.P4.Wearable.Devices;

public enum WearableAlertType { HeartRateAnomaly, FallDetected, InactivityAlert, LowBattery, GeofenceViolation, SosTriggered, TemperatureAnomaly, BloodOxygenLow }
public enum WearableAlertSeverity { Info, Warning, Critical, Emergency }
public enum WearableAlertStatus { Active, Acknowledged, Resolved, Dismissed, Escalated }
public enum CheckinType { Automatic, Manual, Scheduled, SosResponse }

public class WearableAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public WearableAlertType AlertType { get; set; }
    public WearableAlertSeverity Severity { get; set; } = WearableAlertSeverity.Warning;
    public WearableAlertStatus Status { get; set; } = WearableAlertStatus.Active;
    public string Message { get; set; } = string.Empty;
    public double? MeasuredValue { get; set; }
    public double? ThresholdValue { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class WearableCheckin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public CheckinType CheckinType { get; set; } = CheckinType.Automatic;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
    public int? HeartRate { get; set; }
    public int? StepCount { get; set; }
    public int? BatteryPercent { get; set; }
    public bool IsEmergency { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class WearableVoiceTrigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public string TriggerPhrase { get; set; } = string.Empty;
    public string? DetectedPhrase { get; set; }
    public double ConfidenceScore { get; set; }
    public string? ActionTaken { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool WasEmergency { get; set; }
    public bool WasFalsePositive { get; set; }
    public int? BatteryPercentAtTrigger { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
