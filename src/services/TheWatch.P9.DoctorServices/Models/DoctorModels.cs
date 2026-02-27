namespace TheWatch.P9.DoctorServices.Doctors;

public enum AppointmentType
{
    InPerson,
    Telehealth,
    HomeVisit,
    FollowUp
}

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}

public class DoctorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<string> Specializations { get; set; } = [];
    public string? LicenseNumber { get; set; }
    public double Rating { get; set; } = 5.0;
    public int ReviewCount { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool AcceptingPatients { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TelehealthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public string RoomUrl { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

// Request/response records

public record CreateDoctorProfileRequest(
    string Name,
    List<string> Specializations,
    string? LicenseNumber = null,
    string? Phone = null,
    string? Email = null,
    double? Latitude = null,
    double? Longitude = null);

public record BookAppointmentRequest(
    Guid DoctorId,
    Guid PatientId,
    DateTime ScheduledAt,
    AppointmentType Type,
    int DurationMinutes = 30,
    string? Notes = null);

public record UpdateAppointmentStatusRequest(AppointmentStatus Status);

public record RescheduleRequest(DateTime NewScheduledAt);

public record DoctorSearchQuery(
    string? Specialization = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusKm = null,
    bool? AcceptingOnly = null);

public record DoctorListResponse(
    List<DoctorProfile> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DoctorSummary(
    Guid Id,
    string Name,
    List<string> Specializations,
    double Rating,
    double? DistanceKm);
