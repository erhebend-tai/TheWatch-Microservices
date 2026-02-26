namespace TheWatch.Contracts.DoctorServices.Models;

public record CreateDoctorProfileRequest(string Name, List<string> Specializations, string? LicenseNumber = null, string? Phone = null, string? Email = null, double? Latitude = null, double? Longitude = null);
public record BookAppointmentRequest(Guid DoctorId, Guid PatientId, DateTime ScheduledAt, AppointmentType Type, int DurationMinutes = 30, string? Notes = null);
public record UpdateAppointmentStatusRequest(AppointmentStatus Status);
public record RescheduleRequest(DateTime NewScheduledAt);
public record DoctorSearchQuery(string? Specialization = null, double? Latitude = null, double? Longitude = null, double? RadiusKm = null, bool? AcceptingOnly = null);
