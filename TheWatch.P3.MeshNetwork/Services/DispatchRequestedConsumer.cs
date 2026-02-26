using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.Shared.Events;

namespace TheWatch.P3.MeshNetwork.Services;

/// <summary>
/// Consumes DispatchRequested events from P2 to broadcast mesh alert to nearby nodes.
/// </summary>
public sealed class DispatchRequestedConsumer : KafkaEventConsumer<DispatchRequestedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DispatchRequestedConsumer> _logger;

    public DispatchRequestedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        INotificationService notificationService,
        ILogger<DispatchRequestedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.DispatchRequested,
            KafkaSetup.ConsumerGroup)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task HandleAsync(DispatchRequestedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Received DispatchRequested: dispatch={DispatchId} incident={IncidentId} radius={Radius}km responders={Count}",
            evt.DispatchId, evt.IncidentId, evt.RadiusKm, evt.RespondersRequested);

        // Broadcast emergency alert through mesh network
        var alert = new Mesh.SendMessageRequest(
            SenderId: Guid.Empty, // System message
            Content: $"EMERGENCY DISPATCH: Incident {evt.IncidentId} — {evt.RespondersRequested} responders needed within {evt.RadiusKm}km",
            RecipientId: null,
            ChannelId: null,
            Priority: Mesh.MessagePriority.Emergency);

        await _notificationService.SendAsync(alert);

        _logger.LogInformation(
            "Mesh alert broadcast for dispatch {DispatchId}",
            evt.DispatchId);
    }
}
