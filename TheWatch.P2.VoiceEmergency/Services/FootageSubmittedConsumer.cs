using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Events;

namespace TheWatch.P2.VoiceEmergency.Services;

/// <summary>
/// Consumes FootageSubmitted events from P11 to correlate footage with active incidents.
/// </summary>
public sealed class FootageSubmittedConsumer : KafkaEventConsumer<FootageSubmittedEvent>
{
    private readonly IEmergencyService _emergencyService;
    private readonly ILogger<FootageSubmittedConsumer> _logger;

    public FootageSubmittedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        IEmergencyService emergencyService,
        ILogger<FootageSubmittedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.FootageSubmitted,
            KafkaSetup.ConsumerGroup)
    {
        _emergencyService = emergencyService;
        _logger = logger;
    }

    protected override async Task HandleAsync(FootageSubmittedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Received FootageSubmitted: {FootageId} from camera {CameraId} at ({Lat},{Lon})",
            evt.FootageId, evt.CameraId, evt.GpsLatitude, evt.GpsLongitude);

        // Correlate submitted footage with nearby active incidents
        var nearbyIncidents = await _emergencyService.FindIncidentsNearLocationAsync(
            evt.GpsLatitude, evt.GpsLongitude, radiusKm: 2.0);

        if (nearbyIncidents.Count > 0)
        {
            _logger.LogInformation(
                "Footage {FootageId} correlates with {Count} active incidents near ({Lat},{Lon})",
                evt.FootageId, nearbyIncidents.Count, evt.GpsLatitude, evt.GpsLongitude);
        }
    }
}
