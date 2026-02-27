using System.ComponentModel.DataAnnotations;

namespace TheWatch.P7.FamilyHealth.Family;

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

public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
    Emergency
}

public class FamilyGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<Guid> MemberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FamilyMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public FamilyRole Role { get; set; }
    public Guid FamilyGroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public class CheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public CheckInStatus Status { get; set; } = CheckInStatus.Safe;
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class VitalReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public VitalType Type { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class MedicalAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Request/response records

public record CreateFamilyGroupRequest(string Name);

public record AddMemberRequest(
    string Name,
    FamilyRole Role,
    string? Email = null,
    string? Phone = null);

public record CreateCheckInRequest(
    CheckInStatus Status,
    string? Message = null,
    double? Latitude = null,
    double? Longitude = null);

public record RecordVitalRequest(
    VitalType Type,
    double Value,
    string? Unit = null);

public record FamilyGroupResponse(
    FamilyGroup Group,
    List<FamilyMember> Members);

public record MemberCheckInHistory(
    FamilyMember Member,
    List<CheckIn> CheckIns);

public record VitalHistory(
    List<VitalReading> Readings,
    int TotalCount);
