using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<Appointment> _appointments;
    private readonly IWatchRepository<TelehealthSession> _sessions;

    public AppointmentService(IWatchRepository<Appointment> appointments, IWatchRepository<TelehealthSession> sessions)
    {
        _appointments = appointments;
        _sessions = sessions;
    }

    public async Task<Appointment> BookAsync(BookAppointmentRequest request)
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

        return await _appointments.AddAsync(appt);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        return await _appointments.GetByIdAsync(id);
    }

    public async Task<List<Appointment>> ListUpcomingAsync(Guid? doctorId, Guid? patientId)
    {
        var query = _appointments.Query()
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        return await query.OrderBy(a => a.ScheduledAt).ToListAsync();
    }

    public async Task<Appointment?> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request)
    {
        var appt = await _appointments.GetByIdAsync(id);
        if (appt is null) return null;

        appt.Status = request.Status;
        await _appointments.UpdateAsync(appt);
        return appt;
    }

    public async Task<Appointment?> RescheduleAsync(Guid id, RescheduleRequest request)
    {
        var appt = await _appointments.GetByIdAsync(id);
        if (appt is null) return null;

        appt.ScheduledAt = request.NewScheduledAt;
        appt.Status = AppointmentStatus.Scheduled;
        await _appointments.UpdateAsync(appt);
        return appt;
    }

    public async Task<TelehealthSession> CreateSessionAsync(Guid appointmentId)
    {
        var session = new TelehealthSession
        {
            AppointmentId = appointmentId,
            RoomUrl = $"https://telehealth.thewatch.app/room/{Guid.NewGuid():N}",
            StartedAt = DateTime.UtcNow
        };

        return await _sessions.AddAsync(session);
    }

    public async Task CleanupPastAppointmentsAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var oldIds = await _appointments.Query()
            .Where(a => a.ScheduledAt < cutoff && a.Status == AppointmentStatus.Completed)
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var id in oldIds)
            await _appointments.DeleteAsync(id);
    }
}
