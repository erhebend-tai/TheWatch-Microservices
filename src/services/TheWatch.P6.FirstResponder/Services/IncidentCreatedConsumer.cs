using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Events;

namespace TheWatch.P6.FirstResponder.Services;

/// <summary>
/// Consumes IncidentCreated events from P2 to auto-query nearby responders.
/// </summary>
public sealed class IncidentCreatedConsumer : KafkaEventConsumer<IncidentCreatedEvent>
{
    private readonly IResponderService _responderService;
    private readonly ILogger<IncidentCreatedConsumer> _logger;

    public IncidentCreatedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        IResponderService responderService,
        ILogger<IncidentCreatedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.IncidentCreated,
            KafkaSetup.ConsumerGroup)
    {
        _responderService = responderService;
        _logger = logger;
    }

    protected override async Task HandleAsync(IncidentCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Received IncidentCreated: {IncidentId} type={Type} severity={Severity} at ({Lat},{Lon})",
            evt.IncidentId, evt.EmergencyType, evt.Severity, evt.Latitude, evt.Longitude);

        // Auto-query nearby available responders within default radius
        var radiusKm = evt.Severity >= 4 ? 20.0 : 10.0;
        var query = new Responders.NearbyResponderQuery(
            evt.Latitude, evt.Longitude, radiusKm, null, true);

        var nearby = await _responderService.FindNearbyAsync(query);

        _logger.LogInformation(
            "Found {Count} nearby responders for incident {IncidentId} within {Radius}km",
            nearby.Count, evt.IncidentId, radiusKm);
    }
}
