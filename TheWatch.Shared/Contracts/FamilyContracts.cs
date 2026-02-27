using System.ComponentModel.DataAnnotations;

namespace TheWatch.Shared.Contracts.Mobile;

// Shared family health DTOs for mobile client consumption.

public enum FamilyRole
{
    Parent,
    Guardian,
    Child,
    Dependent,
    ElderlyRelative
}

public enum CheckInStatus
{
    Safe,
    NeedHelp,
    Emergency,
    NoResponse
}

public enum VitalType
{
    HeartRate,
    BloodPressure,
    Temperature,
    SpO2,
    RespiratoryRate,
    BloodGlucose
}

public record FamilyGroupDto(
    Guid Id,
    string Name,
    List<FamilyMemberDto> Members);

public record FamilyMemberDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    FamilyRole Role,
    Guid FamilyGroupId);

public record CheckInDto(
    Guid Id,
    Guid MemberId,
    double? Latitude,
    double? Longitude,
    CheckInStatus Status,
    string? Message,
    DateTime Timestamp);

public record VitalReadingDto(
    Guid Id,
    Guid MemberId,
    VitalType Type,
    double Value,
    string? Unit,
    DateTime Timestamp);

public record CreateCheckInRequest(
    [property: Required] CheckInStatus Status,
    [property: MaxLength(500)] string? Message = null,
    [property: Range(-90.0, 90.0)] double? Latitude = null,
    [property: Range(-180.0, 180.0)] double? Longitude = null);

public record RecordVitalRequest(
    [property: Required] VitalType Type,
    [property: Range(0.0, 1000.0)] double Value,
    [property: MaxLength(50)] string? Unit = null);

public record MedicalAlertDto(
    Guid Id,
    Guid MemberId,
    string AlertType,
    string Description,
    string Severity,
    bool Acknowledged,
    DateTime CreatedAt);
