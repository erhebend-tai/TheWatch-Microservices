namespace TheWatch.Shared.Contracts.Mobile;

// Shared emergency DTOs for mobile client consumption.

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

public record LocationDto(
    double Latitude,
    double Longitude,
    double? Accuracy = null,
    DateTime? Timestamp = null);

public record CreateIncidentRequest(
    EmergencyType Type,
    string Description,
    LocationDto Location,
    Guid ReporterId,
    string? ReporterName = null,
    string? ReporterPhone = null,
    int Severity = 3,
    List<string>? Tags = null);

public record IncidentDto(
    Guid Id,
    EmergencyType Type,
    string Description,
    LocationDto Location,
    IncidentStatus Status,
    Guid ReporterId,
    string? ReporterName,
    int Severity,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);

public record IncidentListResponse(
    List<IncidentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record CreateDispatchRequest(
    Guid IncidentId,
    double RadiusKm = 5.0,
    int RespondersRequested = 8);
