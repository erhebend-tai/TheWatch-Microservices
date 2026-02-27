using System.ComponentModel.DataAnnotations;

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

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
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

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public record CreateDispatchRequest(
    Guid IncidentId,
    double RadiusKm = 5.0,
    int RespondersRequested = 8);

public record ExpandRadiusRequest(
    double AdditionalKm = 5.0);

/// <summary>
/// Pre-arrival triage intake captured via text or speech-to-text before the ambulance arrives.
/// Persisted so responders have the caller's symptoms and matched guidance when they arrive.
/// </summary>
public class TriageIntake
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? IncidentId { get; set; }
    public Guid ReporterId { get; set; }

    /// <summary>Raw symptom text entered by the caller (typed or speech-transcribed).</summary>
    public string Symptoms { get; set; } = string.Empty;

    /// <summary>"text" or "stt" — indicates how the symptoms were captured.</summary>
    public string InputMethod { get; set; } = "text";

    /// <summary>Substance/condition name parsed from the symptom text (e.g. "acetaminophen").</summary>
    public string? SubstanceName { get; set; }

    /// <summary>First-aid guidance text returned from the on-device medical reference lookup.</summary>
    public string? MatchedGuidance { get; set; }

    /// <summary>0 = unknown / not assessed; 1-5 matches incident severity scale.</summary>
    public int TriageSeverity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public record LogTriageIntakeRequest(
    Guid ReporterId,
    string Symptoms,
    string InputMethod = "text",
    Guid? IncidentId = null,
    string? SubstanceName = null,
    string? MatchedGuidance = null,
    int TriageSeverity = 0);
