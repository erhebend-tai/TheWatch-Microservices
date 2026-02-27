using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Events;

/// <summary>
/// Logs all event publish/consume operations for audit trail.
/// </summary>
public interface IEventAuditLogger
{
    Task LogAsync(EventAuditEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Default implementation that writes audit entries to structured logs.
/// Can be replaced with a database-backed logger when EF is available.
/// </summary>
public sealed class SerilogEventAuditLogger : IEventAuditLogger
{
    private readonly ILogger<SerilogEventAuditLogger> _logger;

    public SerilogEventAuditLogger(ILogger<SerilogEventAuditLogger> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(EventAuditEntry entry, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EventAudit] {Direction} {EventType} EventId={EventId} Topic={Topic} Source={SourceService} Consumer={ConsumerGroup} Error={Error}",
            entry.Direction,
            entry.EventType,
            entry.EventId,
            entry.Topic,
            entry.SourceService,
            entry.ConsumerGroup ?? "-",
            entry.Error ?? "-");

        return Task.CompletedTask;
    }
}
