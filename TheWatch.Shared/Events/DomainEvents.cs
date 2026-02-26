namespace TheWatch.Shared.Events;

/// <summary>
/// Base class for all domain events flowing through the Kafka event bus.
/// </summary>
public abstract record WatchDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string SourceService { get; init; } = string.Empty;
    public string EventType => GetType().Name;
}

// ── P2 VoiceEmergency events ──

public record IncidentCreatedEvent : WatchDomainEvent
{
    public Guid IncidentId { get; init; }
    public string EmergencyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public Guid ReporterId { get; init; }
    public int Severity { get; init; }
}

public record DispatchRequestedEvent : WatchDomainEvent
{
    public Guid DispatchId { get; init; }
    public Guid IncidentId { get; init; }
    public double RadiusKm { get; init; }
    public int RespondersRequested { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

// ── Dead letter envelope ──

public record DeadLetterEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string OriginalTopic { get; init; } = string.Empty;
    public string OriginalKey { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    public string ConsumerGroup { get; init; } = string.Empty;
}

// ── Audit log entry ──

public record EventAuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string SourceService { get; init; } = string.Empty;
    public string Topic { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty; // "Published" or "Consumed"
    public string? ConsumerGroup { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? Error { get; init; }
}
