using System.ComponentModel.DataAnnotations;

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
    [property: Range(-90.0, 90.0)] double Latitude,
    [property: Range(-180.0, 180.0)] double Longitude,
    double? Accuracy = null,
    DateTime? Timestamp = null);

public record CreateIncidentRequest(
    [property: Required] EmergencyType Type,
    [property: Required, MaxLength(2000)] string Description,
    [property: Required] LocationDto Location,
    [property: Required] Guid ReporterId,
    [property: MaxLength(255)] string? ReporterName = null,
    [property: MaxLength(20)] string? ReporterPhone = null,
    [property: Range(1, 5)] int Severity = 3,
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
    [property: Required] Guid IncidentId,
    [property: Range(0.1, 100.0)] double RadiusKm = 5.0,
    [property: Range(1, 50)] int RespondersRequested = 8);
