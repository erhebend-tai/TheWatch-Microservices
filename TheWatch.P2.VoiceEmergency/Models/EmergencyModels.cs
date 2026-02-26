namespace TheWatch.P2.VoiceEmergency.Emergency;

public enum EmergencyType
{
    Wildfire,
    Hurricane,
    Tornado,
    Flood,
    Earthquake,
    TerroristThreat,
    ChemicalHazard,
    MedicalEmergency,
    ActiveShooter,
    Other
}

public enum IncidentStatus
{
    Reported,
    Dispatched,
    InProgress,
    Resolved,
    Archived,
    Cancelled
}

public enum DispatchStatus
{
    Pending,
    Acknowledged,
    EnRoute,
    OnScene,
    Completed,
    Escalated,
    TimedOut
}

public record Location(
    double Latitude,
    double Longitude,
    double? Accuracy = null,
    DateTime? Timestamp = null);

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EmergencyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Location Location { get; set; } = new(0, 0);
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;
    public Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public int Severity { get; set; } = 3; // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> MediaUrls { get; set; } = [];
}

public record CreateIncidentRequest(
    EmergencyType Type,
    string Description,
    Location Location,
    Guid ReporterId,
    string? ReporterName = null,
    string? ReporterPhone = null,
    int Severity = 3,
    List<string>? Tags = null);

public record UpdateIncidentStatusRequest(
    IncidentStatus Status,
    string? Reason = null);

public record IncidentListResponse(
    List<Incident> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class Dispatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public DispatchStatus Status { get; set; } = DispatchStatus.Pending;
    public double RadiusKm { get; set; } = 5.0;
    public int RespondersRequested { get; set; } = 8;
    public List<Guid> RespondersNotified { get; set; } = [];
    public List<Guid> RespondersAccepted { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public int EscalationCount { get; set; }
}

public record CreateDispatchRequest(
    Guid IncidentId,
    double RadiusKm = 5.0,
    int RespondersRequested = 8);

public record ExpandRadiusRequest(
    double AdditionalKm = 5.0);
