using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.DoctorServices.Models;

namespace TheWatch.Contracts.DoctorServices;

public class DoctorServicesClient(HttpClient http) : ServiceClientBase(http, "DoctorServices"), IDoctorServicesClient
{
    public Task<DoctorProfileDto> GetDoctorAsync(Guid id, CancellationToken ct)
        => GetAsync<DoctorProfileDto>($"/api/doctors/{id}", ct);

    public Task<DoctorListResponse> ListDoctorsAsync(int page, int pageSize, CancellationToken ct)
        => GetAsync<DoctorListResponse>($"/api/doctors?page={page}&pageSize={pageSize}", ct);

    public Task<DoctorProfileDto> CreateDoctorAsync(CreateDoctorProfileRequest request, CancellationToken ct)
        => PostAsync<DoctorProfileDto>("/api/doctors", request, ct);

    public Task<List<DoctorSummary>> SearchDoctorsAsync(DoctorSearchQuery query, CancellationToken ct)
    {
        var q = "/api/doctors/search?";
        if (query.Specialization is not null) q += $"specialization={query.Specialization}&";
        if (query.Latitude.HasValue) q += $"lat={query.Latitude}&";
        if (query.Longitude.HasValue) q += $"lon={query.Longitude}&";
        if (query.RadiusKm.HasValue) q += $"radius={query.RadiusKm}&";
        if (query.AcceptingOnly.HasValue) q += $"acceptingOnly={query.AcceptingOnly}&";
        return GetAsync<List<DoctorSummary>>(q.TrimEnd('&', '?'), ct);
    }

    public Task<AppointmentDto> BookAppointmentAsync(BookAppointmentRequest request, CancellationToken ct)
        => PostAsync<AppointmentDto>("/api/appointments", request, ct);

    public Task<AppointmentDto> GetAppointmentAsync(Guid id, CancellationToken ct)
        => GetAsync<AppointmentDto>($"/api/appointments/{id}", ct);

    public Task<AppointmentDto> UpdateAppointmentStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken ct)
        => PutAsync<AppointmentDto>($"/api/appointments/{id}/status", request, ct);

    public Task<AppointmentDto> RescheduleAsync(Guid id, RescheduleRequest request, CancellationToken ct)
        => PutAsync<AppointmentDto>($"/api/appointments/{id}/reschedule", request, ct);

    public Task<TelehealthSessionDto> GetTelehealthSessionAsync(Guid appointmentId, CancellationToken ct)
        => GetAsync<TelehealthSessionDto>($"/api/appointments/{appointmentId}/telehealth", ct);
}
