using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Services;

/// <summary>
/// Vital sign anomaly detection with configurable thresholds.
/// Uses a sliding-window Z-score algorithm to detect values that deviate
/// significantly from a member's personal baseline, plus hard clinical limits.
/// </summary>
public interface IVitalAnomalyDetectionService
{
    /// <summary>
    /// Evaluate a new vital reading against the member's history.
    /// Returns anomaly details if the reading is outside normal range.
    /// </summary>
    Task<VitalAnomalyResult> EvaluateReadingAsync(VitalReading reading, IReadOnlyList<VitalReading> recentHistory);

    /// <summary>
    /// Get the configured thresholds for a vital type.
    /// </summary>
    VitalThresholds GetThresholds(VitalType type);
}

public record VitalAnomalyResult
{
    public bool IsAnomaly { get; init; }
    public AnomalyType Type { get; init; }
    public AlertSeverity Severity { get; init; }
    public double Value { get; init; }
    public double? BaselineMean { get; init; }
    public double? ZScore { get; init; }
    public string Description { get; init; } = string.Empty;
    public VitalType VitalType { get; init; }
    public Guid MemberId { get; init; }
}

public enum AnomalyType
{
    None,
    AboveHighLimit,       // Exceeds hard clinical upper limit
    BelowLowLimit,        // Below hard clinical lower limit
    StatisticalOutlier,   // Z-score deviation from personal baseline
    RapidChange           // Large delta from previous reading
}

/// <summary>
/// Configurable thresholds per vital type. Clinical defaults from AHA/WHO guidelines.
/// </summary>
public record VitalThresholds
{
    public VitalType Type { get; init; }
    public double CriticalLow { get; init; }
    public double WarningLow { get; init; }
    public double WarningHigh { get; init; }
    public double CriticalHigh { get; init; }
    public double ZScoreThreshold { get; init; } = 2.5;
    public double RapidChangePct { get; init; } = 0.25; // 25% change = rapid
    public string Unit { get; init; } = string.Empty;
}

public class VitalAnomalyDetectionService : IVitalAnomalyDetectionService
{
    private readonly ILogger<VitalAnomalyDetectionService> _logger;

    // Clinical threshold defaults (configurable via IConfiguration in production)
    private static readonly Dictionary<VitalType, VitalThresholds> DefaultThresholds = new()
    {
        [VitalType.HeartRate] = new VitalThresholds
        {
            Type = VitalType.HeartRate, Unit = "bpm",
            CriticalLow = 40, WarningLow = 50, WarningHigh = 100, CriticalHigh = 150,
            ZScoreThreshold = 2.5, RapidChangePct = 0.30
        },
        [VitalType.BloodPressure] = new VitalThresholds
        {
            Type = VitalType.BloodPressure, Unit = "mmHg (systolic)",
            CriticalLow = 70, WarningLow = 90, WarningHigh = 140, CriticalHigh = 180,
            ZScoreThreshold = 2.0, RapidChangePct = 0.20
        },
        [VitalType.Temperature] = new VitalThresholds
        {
            Type = VitalType.Temperature, Unit = "°F",
            CriticalLow = 95.0, WarningLow = 96.8, WarningHigh = 99.5, CriticalHigh = 103.0,
            ZScoreThreshold = 2.0, RapidChangePct = 0.02
        },
        [VitalType.SpO2] = new VitalThresholds
        {
            Type = VitalType.SpO2, Unit = "%",
            CriticalLow = 88, WarningLow = 92, WarningHigh = 100.1, CriticalHigh = 100.1,
            ZScoreThreshold = 2.0, RapidChangePct = 0.05
        },
        [VitalType.RespiratoryRate] = new VitalThresholds
        {
            Type = VitalType.RespiratoryRate, Unit = "breaths/min",
            CriticalLow = 8, WarningLow = 12, WarningHigh = 20, CriticalHigh = 30,
            ZScoreThreshold = 2.5, RapidChangePct = 0.30
        },
        [VitalType.BloodGlucose] = new VitalThresholds
        {
            Type = VitalType.BloodGlucose, Unit = "mg/dL",
            CriticalLow = 54, WarningLow = 70, WarningHigh = 180, CriticalHigh = 300,
            ZScoreThreshold = 2.5, RapidChangePct = 0.40
        }
    };

    public VitalAnomalyDetectionService(ILogger<VitalAnomalyDetectionService> logger)
    {
        _logger = logger;
    }

    public VitalThresholds GetThresholds(VitalType type) =>
        DefaultThresholds.GetValueOrDefault(type, new VitalThresholds { Type = type });

    public Task<VitalAnomalyResult> EvaluateReadingAsync(
        VitalReading reading,
        IReadOnlyList<VitalReading> recentHistory)
    {
        var thresholds = GetThresholds(reading.Type);

        // Check 1: Hard clinical limits
        if (reading.Value <= thresholds.CriticalLow)
        {
            return AnomalyResult(reading, AnomalyType.BelowLowLimit, AlertSeverity.Critical,
                $"{reading.Type} critically low: {reading.Value} {thresholds.Unit} (limit: {thresholds.CriticalLow})");
        }
        if (reading.Value >= thresholds.CriticalHigh)
        {
            return AnomalyResult(reading, AnomalyType.AboveHighLimit, AlertSeverity.Critical,
                $"{reading.Type} critically high: {reading.Value} {thresholds.Unit} (limit: {thresholds.CriticalHigh})");
        }
        if (reading.Value <= thresholds.WarningLow)
        {
            return AnomalyResult(reading, AnomalyType.BelowLowLimit, AlertSeverity.Warning,
                $"{reading.Type} below normal: {reading.Value} {thresholds.Unit} (normal: >{thresholds.WarningLow})");
        }
        if (reading.Value >= thresholds.WarningHigh)
        {
            return AnomalyResult(reading, AnomalyType.AboveHighLimit, AlertSeverity.Warning,
                $"{reading.Type} above normal: {reading.Value} {thresholds.Unit} (normal: <{thresholds.WarningHigh})");
        }

        // Check 2: Statistical outlier from personal baseline
        var sameTypeHistory = recentHistory
            .Where(r => r.Type == reading.Type)
            .OrderByDescending(r => r.Timestamp)
            .Take(50)
            .ToList();

        if (sameTypeHistory.Count >= 5)
        {
            var values = sameTypeHistory.Select(r => r.Value).ToList();
            var mean = values.Average();
            var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));

            if (stdDev > 0.001)
            {
                var zScore = Math.Abs((reading.Value - mean) / stdDev);
                if (zScore >= thresholds.ZScoreThreshold)
                {
                    return AnomalyResult(reading, AnomalyType.StatisticalOutlier, AlertSeverity.Warning,
                        $"{reading.Type} statistical outlier: {reading.Value} {thresholds.Unit} (baseline: {mean:F1}±{stdDev:F1}, Z={zScore:F1})",
                        mean, zScore);
                }
            }

            // Check 3: Rapid change from last reading
            var lastReading = sameTypeHistory.FirstOrDefault();
            if (lastReading is not null && lastReading.Value > 0)
            {
                var changePct = Math.Abs(reading.Value - lastReading.Value) / lastReading.Value;
                if (changePct >= thresholds.RapidChangePct)
                {
                    var severity = changePct >= thresholds.RapidChangePct * 2 ? AlertSeverity.Warning : AlertSeverity.Info;
                    return AnomalyResult(reading, AnomalyType.RapidChange, severity,
                        $"{reading.Type} rapid change: {lastReading.Value:F1}→{reading.Value:F1} {thresholds.Unit} ({changePct:P0} change)");
                }
            }
        }

        // Normal
        return Task.FromResult(new VitalAnomalyResult
        {
            IsAnomaly = false,
            Type = AnomalyType.None,
            Severity = AlertSeverity.Info,
            Value = reading.Value,
            VitalType = reading.Type,
            MemberId = reading.MemberId,
            Description = $"{reading.Type} normal: {reading.Value}"
        });
    }

    private Task<VitalAnomalyResult> AnomalyResult(
        VitalReading reading, AnomalyType type, AlertSeverity severity, string description,
        double? baselineMean = null, double? zScore = null)
    {
        _logger.LogWarning("Vital anomaly for member {MemberId}: {Description}", reading.MemberId, description);
        return Task.FromResult(new VitalAnomalyResult
        {
            IsAnomaly = true,
            Type = type,
            Severity = severity,
            Value = reading.Value,
            BaselineMean = baselineMean,
            ZScore = zScore,
            Description = description,
            VitalType = reading.Type,
            MemberId = reading.MemberId
        });
    }
}
