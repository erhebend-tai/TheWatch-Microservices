using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Events;

/// <summary>
/// Kafka-backed implementation of IEventPublisher using Confluent.Kafka producer.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IEventAuditLogger _auditLogger;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly string _serviceName;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        IEventAuditLogger auditLogger,
        ILogger<KafkaEventPublisher> logger,
        string serviceName)
    {
        _producer = producer;
        _auditLogger = auditLogger;
        _logger = logger;
        _serviceName = serviceName;
    }

    public Task PublishAsync<T>(string topic, T domainEvent, CancellationToken ct = default)
        where T : WatchDomainEvent
        => PublishAsync(topic, domainEvent.EventId.ToString(), domainEvent, ct);

    public async Task PublishAsync<T>(string topic, string key, T domainEvent, CancellationToken ct = default)
        where T : WatchDomainEvent
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOpts);
        var message = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = new Headers
            {
                { "event-type", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventType) },
                { "source-service", System.Text.Encoding.UTF8.GetBytes(_serviceName) },
                { "event-id", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventId.ToString()) }
            }
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, ct);
            _logger.LogInformation(
                "Published {EventType} to {Topic} partition {Partition} offset {Offset}",
                domainEvent.EventType, topic, result.Partition.Value, result.Offset.Value);

            await _auditLogger.LogAsync(new EventAuditEntry
            {
                EventType = domainEvent.EventType,
                EventId = domainEvent.EventId,
                SourceService = _serviceName,
                Topic = topic,
                Direction = "Published"
            });
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} to {Topic}", domainEvent.EventType, topic);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
