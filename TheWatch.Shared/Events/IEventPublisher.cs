namespace TheWatch.Shared.Events;

/// <summary>
/// Publishes domain events to the Kafka event bus.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish a domain event to the specified topic.
    /// The event key defaults to EventId for partitioning.
    /// </summary>
    Task PublishAsync<T>(string topic, T domainEvent, CancellationToken ct = default)
        where T : WatchDomainEvent;

    /// <summary>
    /// Publish a domain event with an explicit partition key.
    /// </summary>
    Task PublishAsync<T>(string topic, string key, T domainEvent, CancellationToken ct = default)
        where T : WatchDomainEvent;
}
