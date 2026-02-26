namespace TheWatch.P1.CoreGateway.Measurements;

public enum MeasurementDomain { Physical, Chemical, Biological, Environmental, Medical, Signal, Wave }
public enum UnitType { Base, Derived, Custom }
public enum DeviceType { Sensor, Meter, Analyzer, Monitor, Wearable, Station, Probe }
public enum DeviceStatus { Active, Calibrating, Maintenance, Offline, Decommissioned }
public enum SignalType { Analog, Digital, Frequency, Amplitude, Phase, Modulated }
public enum WaveType { Electromagnetic, Sound, Seismic, Water, Light, Radio }

public class MeasurementCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MeasurementDomain Domain { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class UnitSystem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMetric { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MeasurementUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UnitSystemId { get; set; }
    public double ConversionFactor { get; set; } = 1.0;
    public double ConversionOffset { get; set; }
    public Guid? BaseUnitId { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MeasurementRange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double? WarningLow { get; set; }
    public double? WarningHigh { get; set; }
    public double? CriticalLow { get; set; }
    public double? CriticalHigh { get; set; }
    public string? Context { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MeasuringDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DeviceType DeviceType { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Active;
    public Guid? PrimaryCategoryId { get; set; }
    public double? Accuracy { get; set; }
    public double? Precision { get; set; }
    public string? CalibrationSchedule { get; set; }
    public DateTime? LastCalibratedAt { get; set; }
    public DateTime? NextCalibrationAt { get; set; }
    public string? FirmwareVersion { get; set; }
    public Guid? OwnerId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class SignalMeasurement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public SignalType SignalType { get; set; }
    public double Value { get; set; }
    public Guid UnitId { get; set; }
    public double? Frequency { get; set; }
    public double? Amplitude { get; set; }
    public double? Phase { get; set; }
    public double? SignalToNoiseRatio { get; set; }
    public double? Bandwidth { get; set; }
    public string? Source { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class WaveMeasurement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public WaveType WaveType { get; set; }
    public double Value { get; set; }
    public Guid UnitId { get; set; }
    public double? Wavelength { get; set; }
    public double? Frequency { get; set; }
    public double? Amplitude { get; set; }
    public double? Period { get; set; }
    public double? Velocity { get; set; }
    public double? Intensity { get; set; }
    public string? Medium { get; set; }
    public string? Source { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
