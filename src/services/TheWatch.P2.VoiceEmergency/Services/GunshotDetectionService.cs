using TheWatch.Shared.Events;
using TheWatch.Shared.ML;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Services;

/// <summary>
/// Gunshot detection service for P2 active shooter scenarios.
/// Receives audio samples, runs ONNX inference, and auto-creates
/// incidents for confirmed gunshot/explosion detections.
/// </summary>
public interface IGunshotDetectionService
{
    /// <summary>
    /// Analyze an audio sample for gunshot/explosion signatures.
    /// Auto-creates an ActiveShooter incident if confidence exceeds threshold.
    /// </summary>
    Task<GunshotDetectionResult> AnalyzeAudioAsync(
        byte[] audioData,
        Guid reporterId,
        double latitude,
        double longitude,
        int sampleRate = 16000,
        CancellationToken cancellationToken = default);
}

public record GunshotDetectionResult
{
    public bool ThreatDetected { get; init; }
    public string? DetectedLabel { get; init; }
    public float Confidence { get; init; }
    public Guid? IncidentId { get; init; }
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

public class GunshotDetectionService : IGunshotDetectionService
{
    private readonly IAudioClassifier _classifier;
    private readonly IEmergencyService _emergencyService;
    private readonly IEventPublisher _events;
    private readonly ILogger<GunshotDetectionService> _logger;

    private const float DetectionThreshold = 0.75f;

    public GunshotDetectionService(
        IAudioClassifier classifier,
        IEmergencyService emergencyService,
        IEventPublisher events,
        ILogger<GunshotDetectionService> logger)
    {
        _classifier = classifier;
        _emergencyService = emergencyService;
        _events = events;
        _logger = logger;
    }

    public async Task<GunshotDetectionResult> AnalyzeAudioAsync(
        byte[] audioData,
        Guid reporterId,
        double latitude,
        double longitude,
        int sampleRate,
        CancellationToken cancellationToken)
    {
        var classifications = await _classifier.ClassifyAsync(audioData, sampleRate, DetectionThreshold, cancellationToken);

        // Find the highest-confidence danger label
        var threat = classifications
            .Where(c => AudioLabels.DangerLabels.Contains(c.Label))
            .OrderByDescending(c => c.Confidence)
            .FirstOrDefault();

        if (threat is null)
        {
            return new GunshotDetectionResult { ThreatDetected = false };
        }

        _logger.LogWarning("Threat detected: {Label} with {Confidence:P1} confidence at ({Lat}, {Lon})",
            threat.Label, threat.Confidence, latitude, longitude);

        // Auto-create an incident for confirmed threats
        var emergencyType = threat.Label switch
        {
            AudioLabels.Gunshot => EmergencyType.ActiveShooter,
            AudioLabels.Explosion => EmergencyType.TerroristThreat,
            _ => EmergencyType.Other
        };

        var incident = await _emergencyService.CreateIncidentAsync(new CreateIncidentRequest(
            Type: emergencyType,
            Description: $"[ML-AUTO] {threat.Label} detected with {threat.Confidence:P1} confidence. Audio classifier v{threat.ModelVersion}.",
            Location: new Location(latitude, longitude, Timestamp: DateTime.UtcNow),
            ReporterId: reporterId,
            Severity: threat.Label == AudioLabels.Gunshot ? 5 : 4,
            Tags: ["ml-detected", threat.Label, $"confidence:{threat.Confidence:F2}"]
        ));

        // Publish high-priority event
        await _events.PublishAsync(WatchTopics.IncidentCreated, incident.Id.ToString(), new IncidentCreatedEvent
        {
            SourceService = "TheWatch.P2.VoiceEmergency.ML",
            IncidentId = incident.Id,
            EmergencyType = emergencyType.ToString(),
            Description = incident.Description,
            Latitude = latitude,
            Longitude = longitude,
            ReporterId = reporterId,
            Severity = incident.Severity
        });

        return new GunshotDetectionResult
        {
            ThreatDetected = true,
            DetectedLabel = threat.Label,
            Confidence = threat.Confidence,
            IncidentId = incident.Id
        };
    }
}
