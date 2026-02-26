namespace TheWatch.Shared.ML;

/// <summary>
/// Audio classification result from ML inference.
/// </summary>
public record AudioClassificationResult
{
    public string Label { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public DateTime ClassifiedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan AudioDuration { get; init; }
    public string ModelVersion { get; init; } = "1.0.0";
}

/// <summary>
/// Audio classifier interface for ONNX-based sound detection.
/// Used by P2 VoiceEmergency for gunshot/explosion detection.
/// </summary>
public interface IAudioClassifier
{
    /// <summary>
    /// Classify an audio segment. Returns all detected labels above the confidence threshold.
    /// </summary>
    Task<IReadOnlyList<AudioClassificationResult>> ClassifyAsync(
        byte[] audioData,
        int sampleRate = 16000,
        float confidenceThreshold = 0.7f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the classifier model is loaded and ready.
    /// </summary>
    bool IsReady { get; }
}

/// <summary>
/// Well-known audio classification labels for emergency detection.
/// </summary>
public static class AudioLabels
{
    public const string Gunshot = "gunshot";
    public const string Explosion = "explosion";
    public const string Scream = "scream";
    public const string GlassBreak = "glass_break";
    public const string Siren = "siren";
    public const string Background = "background";

    public static readonly IReadOnlySet<string> DangerLabels = new HashSet<string>
    {
        Gunshot, Explosion, Scream, GlassBreak
    };
}
