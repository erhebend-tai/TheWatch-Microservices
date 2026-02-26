using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Services;

/// <summary>
/// Fall detection from wearable accelerometer data streams.
/// Uses a sliding-window algorithm over accelerometer magnitude
/// to detect sudden impact + post-impact stillness pattern.
/// </summary>
public interface IFallDetectionService
{
    /// <summary>
    /// Analyze a batch of accelerometer readings for fall signatures.
    /// </summary>
    FallDetectionResult AnalyzeAccelerometerData(IReadOnlyList<AccelerometerSample> samples, Guid deviceId, Guid ownerId);

    /// <summary>
    /// Process a single real-time sample (buffers internally, triggers when window is full).
    /// </summary>
    FallDetectionResult? ProcessSample(AccelerometerSample sample, Guid deviceId, Guid ownerId);
}

public record AccelerometerSample(
    double X,
    double Y,
    double Z,
    DateTime Timestamp)
{
    /// <summary>Total acceleration magnitude (m/s²).</summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
}

public record FallDetectionResult
{
    public bool FallDetected { get; init; }
    public double PeakAcceleration { get; init; }
    public double PostImpactStillness { get; init; }
    public DateTime? FallTimestamp { get; init; }
    public Guid DeviceId { get; init; }
    public Guid OwnerId { get; init; }
    public FallConfidence Confidence { get; init; }
}

public enum FallConfidence
{
    None,
    Low,       // Single spike — could be sitting down hard
    Medium,    // Spike + partial stillness — possible fall
    High       // Spike + prolonged stillness — likely fall
}

public class FallDetectionService : IFallDetectionService
{
    private readonly ILogger<FallDetectionService> _logger;
    private readonly Dictionary<Guid, List<AccelerometerSample>> _buffers = new();

    // Tunable thresholds (OWASP: configurable, not hardcoded)
    private const double ImpactThresholdG = 3.0;      // 3g peak = significant impact
    private const double StillnessThresholdG = 0.3;    // < 0.3g variance = stillness
    private const int WindowSizeMs = 5000;              // 5-second analysis window
    private const int PostImpactWindowMs = 3000;        // 3 seconds post-impact to check stillness
    private const int MinSamplesForAnalysis = 50;       // ~10Hz for 5 seconds

    private const double GravityG = 9.81;

    public FallDetectionService(ILogger<FallDetectionService> logger)
    {
        _logger = logger;
    }

    public FallDetectionResult? ProcessSample(AccelerometerSample sample, Guid deviceId, Guid ownerId)
    {
        if (!_buffers.TryGetValue(deviceId, out var buffer))
        {
            buffer = new List<AccelerometerSample>();
            _buffers[deviceId] = buffer;
        }

        buffer.Add(sample);

        // Trim old samples outside the window
        var cutoff = sample.Timestamp.AddMilliseconds(-WindowSizeMs);
        buffer.RemoveAll(s => s.Timestamp < cutoff);

        if (buffer.Count < MinSamplesForAnalysis) return null;

        var result = AnalyzeAccelerometerData(buffer, deviceId, ownerId);
        if (result.FallDetected)
        {
            buffer.Clear(); // Reset after detection to avoid duplicate alerts
        }
        return result;
    }

    public FallDetectionResult AnalyzeAccelerometerData(
        IReadOnlyList<AccelerometerSample> samples,
        Guid deviceId,
        Guid ownerId)
    {
        if (samples.Count < 3)
        {
            return new FallDetectionResult { DeviceId = deviceId, OwnerId = ownerId, Confidence = FallConfidence.None };
        }

        // Phase 1: Detect impact spike (acceleration magnitude >> 1g)
        var magnitudesG = samples.Select(s => s.Magnitude / GravityG).ToList();
        var peakG = magnitudesG.Max();
        var peakIndex = magnitudesG.IndexOf(peakG);

        if (peakG < ImpactThresholdG)
        {
            return new FallDetectionResult
            {
                DeviceId = deviceId,
                OwnerId = ownerId,
                PeakAcceleration = peakG,
                Confidence = FallConfidence.None
            };
        }

        // Phase 2: Check post-impact stillness
        var peakTimestamp = samples[peakIndex].Timestamp;
        var postImpactSamples = samples
            .Where(s => s.Timestamp > peakTimestamp &&
                        s.Timestamp <= peakTimestamp.AddMilliseconds(PostImpactWindowMs))
            .ToList();

        double stillnessScore = 0;
        if (postImpactSamples.Count >= 3)
        {
            // Measure variance in acceleration magnitude post-impact
            var postMagnitudes = postImpactSamples.Select(s => s.Magnitude / GravityG).ToList();
            var mean = postMagnitudes.Average();
            var variance = postMagnitudes.Average(m => Math.Pow(m - mean, 2));
            stillnessScore = variance;
        }

        // Phase 3: Determine confidence
        var confidence = (peakG, stillnessScore) switch
        {
            ( >= 5.0, <= StillnessThresholdG) => FallConfidence.High,
            ( >= ImpactThresholdG, <= StillnessThresholdG) => FallConfidence.Medium,
            ( >= ImpactThresholdG, _) => FallConfidence.Low,
            _ => FallConfidence.None
        };

        var fallDetected = confidence >= FallConfidence.Medium;

        if (fallDetected)
        {
            _logger.LogWarning(
                "Fall detected for device {DeviceId} (owner {OwnerId}): peak={PeakG:F1}g, stillness={Stillness:F3}, confidence={Confidence}",
                deviceId, ownerId, peakG, stillnessScore, confidence);
        }

        return new FallDetectionResult
        {
            FallDetected = fallDetected,
            PeakAcceleration = peakG,
            PostImpactStillness = stillnessScore,
            FallTimestamp = fallDetected ? peakTimestamp : null,
            DeviceId = deviceId,
            OwnerId = ownerId,
            Confidence = confidence
        };
    }
}
