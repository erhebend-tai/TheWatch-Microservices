using System.Collections.Concurrent;
using TheWatch.P9.DoctorServices.Doctors;

namespace TheWatch.P9.DoctorServices.Services;

public interface IAppointmentService
{
    Task<Appointment> BookAsync(BookAppointmentRequest request);
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<List<Appointment>> ListUpcomingAsync(Guid? doctorId = null, Guid? patientId = null);
    Task<Appointment?> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request);
    Task<Appointment?> RescheduleAsync(Guid id, RescheduleRequest request);
    Task<TelehealthSession> CreateSessionAsync(Guid appointmentId);
    Task CleanupPastAppointmentsAsync(TimeSpan olderThan);
}

public class AppointmentService : IAppointmentService
{
    private readonly ConcurrentDictionary<Guid, Appointment> _appointments = new();
    private readonly ConcurrentDictionary<Guid, TelehealthSession> _sessions = new();

    public Task<Appointment> BookAsync(BookAppointmentRequest request)
    {
        var appt = new Appointment
        {
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            ScheduledAt = request.ScheduledAt,
            Type = request.Type,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes
        };

        _appointments[appt.Id] = appt;
        return Task.FromResult(appt);
    }

    public Task<Appointment?> GetByIdAsync(Guid id)
    {
        _appointments.TryGetValue(id, out var appt);
        return Task.FromResult(appt);
    }

    public Task<List<Appointment>> ListUpcomingAsync(Guid? doctorId, Guid? patientId)
    {
        var query = _appointments.Values
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        return Task.FromResult(query.OrderBy(a => a.ScheduledAt).ToList());
    }

    public Task<Appointment?> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request)
    {
        if (!_appointments.TryGetValue(id, out var appt))
            return Task.FromResult<Appointment?>(null);

        appt.Status = request.Status;
        return Task.FromResult<Appointment?>(appt);
    }

    public Task<Appointment?> RescheduleAsync(Guid id, RescheduleRequest request)
    {
        if (!_appointments.TryGetValue(id, out var appt))
            return Task.FromResult<Appointment?>(null);

        appt.ScheduledAt = request.NewScheduledAt;
        appt.Status = AppointmentStatus.Scheduled;
        return Task.FromResult<Appointment?>(appt);
    }

    public Task<TelehealthSession> CreateSessionAsync(Guid appointmentId)
    {
        var session = new TelehealthSession
        {
            AppointmentId = appointmentId,
            RoomUrl = $"https://telehealth.thewatch.app/room/{Guid.NewGuid():N}",
            StartedAt = DateTime.UtcNow
        };

        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task CleanupPastAppointmentsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = _appointments.Values
            .Where(a => a.ScheduledAt < cutoff && a.Status == AppointmentStatus.Completed)
            .Select(a => a.Id).ToList();
        foreach (var id in old)
            _appointments.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
