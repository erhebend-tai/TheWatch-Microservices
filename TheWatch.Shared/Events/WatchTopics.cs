namespace TheWatch.Shared.Events;

/// <summary>
/// Well-known Kafka topic names for the TheWatch event bus.
/// </summary>
public static class WatchTopics
{
    public const string IncidentCreated = "watch.incidents.created";
    public const string DispatchRequested = "watch.dispatch.requested";

    // P7 FamilyHealth / P4 Wearable
    public const string CheckInCompleted = "watch.health.checkin-completed";
    public const string VitalAlert = "watch.health.vital-alert";

    // P8 DisasterRelief
    public const string DisasterDeclared = "watch.disaster.declared";
    public const string EvacuationOrdered = "watch.disaster.evacuation-ordered";

    // P11 Surveillance
    public const string FootageSubmitted = "watch.surveillance.footage-submitted";
    public const string FootageAnalyzed = "watch.surveillance.footage-analyzed";
    public const string CrimeLocationReported = "watch.surveillance.crime-reported";
    public const string SuspiciousDetection = "watch.surveillance.suspicious-detection";

    // P12 Notifications
    public const string NotificationSent = "watch.notifications.sent";
    public const string NotificationBroadcast = "watch.notifications.broadcast";
}
