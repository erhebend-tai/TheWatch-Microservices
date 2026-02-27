using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Events;

/// <summary>
/// Handles events that failed processing after all retries.
/// </summary>
public interface IDeadLetterHandler
{
    Task HandleAsync(DeadLetterEvent deadLetter, CancellationToken ct = default);
}

/// <summary>
/// Publishes failed events to a dead letter topic (*.dlq) in Kafka.
/// </summary>
public sealed class KafkaDeadLetterHandler : IDeadLetterHandler, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaDeadLetterHandler> _logger;

    public KafkaDeadLetterHandler(
        IProducer<string, string> producer,
        ILogger<KafkaDeadLetterHandler> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task HandleAsync(DeadLetterEvent deadLetter, CancellationToken ct = default)
    {
        var dlqTopic = $"{deadLetter.OriginalTopic}.dlq";
        var payload = System.Text.Json.JsonSerializer.Serialize(deadLetter);

        try
        {
            await _producer.ProduceAsync(dlqTopic, new Message<string, string>
            {
                Key = deadLetter.Id.ToString(),
                Value = payload,
                Headers = new Headers
                {
                    { "original-topic", System.Text.Encoding.UTF8.GetBytes(deadLetter.OriginalTopic) },
                    { "error", System.Text.Encoding.UTF8.GetBytes(deadLetter.Error.Length > 500
                        ? deadLetter.Error[..500] : deadLetter.Error) }
                }
            }, ct);

            _logger.LogWarning(
                "Dead letter published to {DlqTopic} for original topic {OriginalTopic}, key={Key}",
                dlqTopic, deadLetter.OriginalTopic, deadLetter.OriginalKey);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogCritical(ex,
                "Failed to publish to DLQ {DlqTopic} — event permanently lost: {Payload}",
                dlqTopic, payload);
        }
    }

    public void Dispose() => _producer.Dispose();
}
