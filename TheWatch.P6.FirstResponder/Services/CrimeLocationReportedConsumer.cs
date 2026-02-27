using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Events;

namespace TheWatch.P6.FirstResponder.Services;

/// <summary>
/// Consumes CrimeLocationReported events from P11 to alert nearby responders.
/// </summary>
public sealed class CrimeLocationReportedConsumer : KafkaEventConsumer<CrimeLocationReportedEvent>
{
    private readonly IResponderService _responderService;
    private readonly ILogger<CrimeLocationReportedConsumer> _logger;

    public CrimeLocationReportedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        IResponderService responderService,
        ILogger<CrimeLocationReportedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.CrimeLocationReported,
            KafkaSetup.ConsumerGroup)
    {
        _responderService = responderService;
        _logger = logger;
    }

    protected override async Task HandleAsync(CrimeLocationReportedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Received CrimeLocationReported: {CrimeLocationId} type={CrimeType} at ({Lat},{Lon})",
            evt.CrimeLocationId, evt.CrimeType, evt.Latitude, evt.Longitude);

        // Auto-alert nearby responders about reported crime locations
        var query = new Responders.NearbyResponderQuery(
            evt.Latitude, evt.Longitude, radiusKm: 5.0, null, true);

        var nearby = await _responderService.FindNearbyAsync(query);

        _logger.LogInformation(
            "Found {Count} nearby responders for crime location {CrimeLocationId} within 5km",
            nearby.Count, evt.CrimeLocationId);
    }
}
