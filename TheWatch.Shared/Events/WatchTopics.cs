namespace TheWatch.Shared.Events;

/// <summary>
/// Well-known Kafka topic names for the TheWatch event bus.
/// </summary>
public static class WatchTopics
{
    public const string IncidentCreated = "watch.incidents.created";
    public const string DispatchRequested = "watch.dispatch.requested";

    // P11 Surveillance
    public const string FootageSubmitted = "watch.surveillance.footage-submitted";
    public const string FootageAnalyzed = "watch.surveillance.footage-analyzed";
    public const string CrimeLocationReported = "watch.surveillance.crime-reported";
    public const string SuspiciousDetection = "watch.surveillance.suspicious-detection";
}
