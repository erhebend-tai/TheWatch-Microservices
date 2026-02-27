namespace TheWatch.Contracts.FamilyHealth.Models;

public class FamilyGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> MemberIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class FamilyMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public FamilyRole Role { get; set; }
    public Guid FamilyGroupId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CheckInDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public CheckInStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VitalReadingDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public VitalType Type { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MedicalAlertDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime CreatedAt { get; set; }
}
