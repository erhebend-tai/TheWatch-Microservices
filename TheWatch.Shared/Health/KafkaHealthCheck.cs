using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Health;

/// <summary>
/// Custom health check for Apache Kafka connectivity. Creates an ephemeral AdminClient
/// to list topics, verifying that the Kafka cluster is reachable and responding to
/// metadata requests. Failing this check indicates the event bus is down and domain
/// events (incident creation, dispatch, responder alerts) will not propagate.
/// </summary>
/// <remarks>
/// Tagged with "messaging" and "kafka" for selective health check filtering via
/// <c>MapHealthChecks</c> predicate.
/// </remarks>
public sealed class KafkaHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaHealthCheck> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public KafkaHealthCheck(string bootstrapServers, ILogger<KafkaHealthCheck> logger)
    {
        _bootstrapServers = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to connect to the Kafka cluster and retrieve topic metadata.
    /// Returns <see cref="HealthCheckResult.Healthy"/> if topics can be listed,
    /// <see cref="HealthCheckResult.Unhealthy"/> on any failure.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers,
                SocketTimeoutMs = (int)Timeout.TotalMilliseconds,
                MetadataMaxAgeMs = (int)Timeout.TotalMilliseconds
            };

            using var adminClient = new AdminClientBuilder(config).Build();

            // ListTopics is synchronous in Confluent.Kafka — wrap to avoid blocking the thread pool
            var metadata = await Task.Run(
                () => adminClient.GetMetadata(Timeout),
                cancellationToken);

            var topicCount = metadata.Topics.Count;
            var brokerCount = metadata.Brokers.Count;

            _logger.LogDebug(
                "Kafka health check passed: {BrokerCount} brokers, {TopicCount} topics at {BootstrapServers}",
                brokerCount, topicCount, _bootstrapServers);

            return HealthCheckResult.Healthy(
                $"Kafka cluster reachable: {brokerCount} broker(s), {topicCount} topic(s).",
                new Dictionary<string, object>
                {
                    ["brokers"] = brokerCount,
                    ["topics"] = topicCount,
                    ["bootstrapServers"] = _bootstrapServers
                });
        }
        catch (KafkaException ex)
        {
            _logger.LogWarning(ex,
                "Kafka health check failed at {BootstrapServers}: {Error}",
                _bootstrapServers, ex.Error.Reason);

            return HealthCheckResult.Unhealthy(
                $"Kafka cluster unreachable at {_bootstrapServers}: {ex.Error.Reason}",
                ex,
                new Dictionary<string, object>
                {
                    ["bootstrapServers"] = _bootstrapServers,
                    ["errorCode"] = ex.Error.Code.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Kafka health check failed at {BootstrapServers}",
                _bootstrapServers);

            return HealthCheckResult.Unhealthy(
                $"Kafka health check failed: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    ["bootstrapServers"] = _bootstrapServers
                });
        }
    }
}
