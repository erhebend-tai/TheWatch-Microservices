using TheWatch.P12.Notifications.Notifications;
using TheWatch.P12.Notifications.Services;
using TheWatch.Shared.Events;

namespace TheWatch.P12.Notifications.Events;

/// <summary>
/// Consumes IncidentCreatedEvent from P2 VoiceEmergency and dispatches emergency push
/// notifications to responders and family members in proximity.
/// Cross-domain event flow: incident-created → push/SMS notifications (Phase 2, Item 226).
/// </summary>
public sealed class IncidentCreatedNotificationConsumer : KafkaEventConsumer<IncidentCreatedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<IncidentCreatedNotificationConsumer> _logger;

    public IncidentCreatedNotificationConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        INotificationDispatcher dispatcher,
        ILogger<IncidentCreatedNotificationConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.IncidentCreated,
            KafkaSetup.ConsumerGroup)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task HandleAsync(IncidentCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "IncidentCreated notification triggered: incident={IncidentId} type={Type} severity={Severity} at ({Lat},{Lng})",
            evt.IncidentId, evt.EmergencyType, evt.Severity, evt.Latitude, evt.Longitude);

        // Broadcast alert to all community users subscribed to Emergency category
        await _dispatcher.BroadcastAsync(new BroadcastRequest(
            Title: $"Emergency: {evt.EmergencyType}",
            Body: evt.Description,
            Category: NotificationCategory.Emergency,
            Priority: evt.Severity >= 4 ? NotificationPriority.Critical : NotificationPriority.High,
            RecipientIds: null,
            TargetLatitude: evt.Latitude,
            TargetLongitude: evt.Longitude,
            RadiusKm: 5.0,
            SenderId: evt.ReporterId
        ), ct);
    }
}
