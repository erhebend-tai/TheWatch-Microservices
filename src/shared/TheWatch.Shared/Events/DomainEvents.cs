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

// ── P11 Surveillance events ──

public record FootageSubmittedEvent : WatchDomainEvent
{
    public Guid FootageId { get; init; }
    public Guid CameraId { get; init; }
    public double GpsLatitude { get; init; }
    public double GpsLongitude { get; init; }
    public Guid SubmitterId { get; init; }
}

public record FootageAnalyzedEvent : WatchDomainEvent
{
    public Guid FootageId { get; init; }
    public int DetectionCount { get; init; }
    public bool HasSuspiciousActivity { get; init; }
    public string DetectionSummary { get; init; } = string.Empty;
}

public record CrimeLocationReportedEvent : WatchDomainEvent
{
    public Guid CrimeLocationId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string CrimeType { get; init; } = string.Empty;
    public Guid ReporterId { get; init; }
}

public record SuspiciousDetectionEvent : WatchDomainEvent
{
    public Guid FootageId { get; init; }
    public string DetectionType { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public string Label { get; init; } = string.Empty;
    public Guid CameraId { get; init; }
}

// ── P7 FamilyHealth / P4 Wearable events ──

public record CheckInCompletedEvent : WatchDomainEvent
{
    public Guid CheckInId { get; init; }
    public Guid UserId { get; init; }
    public Guid FamilyGroupId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public bool IsOverdue { get; init; }
}

public record VitalAlertEvent : WatchDomainEvent
{
    public Guid AlertId { get; init; }
    public Guid UserId { get; init; }
    public string VitalType { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Severity { get; init; } = string.Empty;
    public bool RequiresImmediate { get; init; }
}

// ── P8 DisasterRelief events ──

public record DisasterDeclaredEvent : WatchDomainEvent
{
    public Guid EventId2 { get; init; }
    public string DisasterType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double CenterLatitude { get; init; }
    public double CenterLongitude { get; init; }
    public double RadiusKm { get; init; }
    public string Severity { get; init; } = string.Empty;
}

public record EvacuationOrderedEvent : WatchDomainEvent
{
    public Guid EvacuationId { get; init; }
    public Guid DisasterEventId { get; init; }
    public string ZoneName { get; init; } = string.Empty;
    public DateTime OrderedAt { get; init; }
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
