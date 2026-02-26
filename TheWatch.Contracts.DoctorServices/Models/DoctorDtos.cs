namespace TheWatch.Contracts.DoctorServices.Models;

public class DoctorProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Specializations { get; set; } = [];
    public string? LicenseNumber { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool AcceptingPatients { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TelehealthSessionDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string RoomUrl { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
