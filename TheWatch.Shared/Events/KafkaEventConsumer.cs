using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Events;

/// <summary>
/// Base class for Kafka event consumers. Subclasses implement HandleAsync
/// and are registered as hosted services to run continuously.
/// </summary>
public abstract class KafkaEventConsumer<TEvent> : BackgroundService
    where TEvent : WatchDomainEvent
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IDeadLetterHandler _deadLetterHandler;
    private readonly IEventAuditLogger _auditLogger;
    private readonly ILogger _logger;
    private readonly string _topic;
    private readonly string _consumerGroup;
    private readonly int _maxRetries;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected KafkaEventConsumer(
        IConsumer<string, string> consumer,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        ILogger logger,
        string topic,
        string consumerGroup,
        int maxRetries = 3)
    {
        _consumer = consumer;
        _deadLetterHandler = deadLetterHandler;
        _auditLogger = auditLogger;
        _logger = logger;
        _topic = topic;
        _consumerGroup = consumerGroup;
        _maxRetries = maxRetries;
    }

    /// <summary>
    /// Process a consumed domain event. Implement per consumer.
    /// </summary>
    protected abstract Task HandleAsync(TEvent domainEvent, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation(
            "Kafka consumer started: topic={Topic} group={Group}",
            _topic, _consumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = _consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on {Topic}", _topic);
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                var attempt = 0;
                var success = false;

                while (attempt < _maxRetries && !success)
                {
                    attempt++;
                    try
                    {
                        var domainEvent = JsonSerializer.Deserialize<TEvent>(result.Message.Value, JsonOpts);
                        if (domainEvent is null)
                        {
                            _logger.LogWarning("Failed to deserialize event from {Topic}", _topic);
                            break;
                        }

                        await HandleAsync(domainEvent, stoppingToken);
                        success = true;

                        await _auditLogger.LogAsync(new EventAuditEntry
                        {
                            EventType = domainEvent.EventType,
                            EventId = domainEvent.EventId,
                            SourceService = domainEvent.SourceService,
                            Topic = _topic,
                            Direction = "Consumed",
                            ConsumerGroup = _consumerGroup
                        });
                    }
                    catch (Exception ex) when (attempt < _maxRetries)
                    {
                        _logger.LogWarning(ex,
                            "Retry {Attempt}/{Max} for event on {Topic}",
                            attempt, _maxRetries, _topic);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to process event on {Topic} after {Max} attempts, sending to DLQ",
                            _topic, _maxRetries);

                        await _deadLetterHandler.HandleAsync(new DeadLetterEvent
                        {
                            OriginalTopic = _topic,
                            OriginalKey = result.Message.Key ?? "",
                            Payload = result.Message.Value,
                            Error = ex.ToString(),
                            AttemptCount = _maxRetries,
                            ConsumerGroup = _consumerGroup
                        });

                        await _auditLogger.LogAsync(new EventAuditEntry
                        {
                            EventType = typeof(TEvent).Name,
                            EventId = Guid.Empty,
                            SourceService = "unknown",
                            Topic = _topic,
                            Direction = "Consumed",
                            ConsumerGroup = _consumerGroup,
                            Error = ex.Message
                        });
                    }
                }

                _consumer.Commit(result);
            }
        }
        finally
        {
            _consumer.Close();
        }
    }
}
