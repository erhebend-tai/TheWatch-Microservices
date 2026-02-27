namespace TheWatch.Contracts.VoiceEmergency.Models;

public record LocationDto(double Latitude, double Longitude, double? Accuracy = null, DateTime? Timestamp = null);

public class IncidentDto
{
    public Guid Id { get; set; }
    public EmergencyType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public LocationDto Location { get; set; } = new(0, 0);
    public IncidentStatus Status { get; set; }
    public Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public int Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> MediaUrls { get; set; } = [];
}

public class DispatchDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public DispatchStatus Status { get; set; }
    public double RadiusKm { get; set; }
    public int RespondersRequested { get; set; }
    public List<Guid> RespondersNotified { get; set; } = [];
    public List<Guid> RespondersAccepted { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public int EscalationCount { get; set; }
}
