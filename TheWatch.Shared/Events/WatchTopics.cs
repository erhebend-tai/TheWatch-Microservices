namespace TheWatch.Shared.Events;

/// <summary>
/// Well-known Kafka topic names for the TheWatch event bus.
/// </summary>
public static class WatchTopics
{
    public const string IncidentCreated = "watch.incidents.created";
    public const string DispatchRequested = "watch.dispatch.requested";
}
