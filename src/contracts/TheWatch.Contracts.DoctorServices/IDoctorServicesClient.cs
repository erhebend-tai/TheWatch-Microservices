using TheWatch.Contracts.DoctorServices.Models;

namespace TheWatch.Contracts.DoctorServices;

public interface IDoctorServicesClient
{
    Task<DoctorProfileDto> GetDoctorAsync(Guid id, CancellationToken ct = default);
    Task<DoctorListResponse> ListDoctorsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<DoctorProfileDto> CreateDoctorAsync(CreateDoctorProfileRequest request, CancellationToken ct = default);
    Task<List<DoctorSummary>> SearchDoctorsAsync(DoctorSearchQuery query, CancellationToken ct = default);
    Task<AppointmentDto> BookAppointmentAsync(BookAppointmentRequest request, CancellationToken ct = default);
    Task<AppointmentDto> GetAppointmentAsync(Guid id, CancellationToken ct = default);
    Task<AppointmentDto> UpdateAppointmentStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken ct = default);
    Task<AppointmentDto> RescheduleAsync(Guid id, RescheduleRequest request, CancellationToken ct = default);
    Task<TelehealthSessionDto> GetTelehealthSessionAsync(Guid appointmentId, CancellationToken ct = default);
}
