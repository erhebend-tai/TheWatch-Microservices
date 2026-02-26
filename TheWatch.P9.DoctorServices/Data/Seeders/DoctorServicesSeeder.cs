using Microsoft.EntityFrameworkCore;
using TheWatch.P9.DoctorServices.Doctors;

namespace TheWatch.P9.DoctorServices.Data.Seeders;

public class DoctorServicesSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(DoctorServicesDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<DoctorProfile>().AnyAsync(ct))
            return;

        // Doctors
        var doctors = new[]
        {
            new DoctorProfile { Id = Guid.Parse("00000000-0000-0000-0009-000000000001"), Name = "Dr. Emily Zhang", Specializations = ["Emergency Medicine", "Trauma Surgery"], LicenseNumber = "EM-12345-CA", Phone = "+15553001001", Email = "e.zhang@watchmed.test", AcceptingPatients = true, Rating = 4.9, ReviewCount = 142, Latitude = 34.0736, Longitude = -118.3716 },
            new DoctorProfile { Id = Guid.Parse("00000000-0000-0000-0009-000000000002"), Name = "Dr. Marcus Johnson", Specializations = ["Family Medicine", "Telemedicine"], LicenseNumber = "FM-67890-CA", Phone = "+15553001002", Email = "m.johnson@watchmed.test", AcceptingPatients = true, Rating = 4.7, ReviewCount = 89, Latitude = 34.0522, Longitude = -118.2437 },
            new DoctorProfile { Id = Guid.Parse("00000000-0000-0000-0009-000000000003"), Name = "Dr. Priya Patel", Specializations = ["Pediatrics", "Developmental Medicine"], LicenseNumber = "PD-11223-CA", Phone = "+15553001003", Email = "p.patel@watchmed.test", AcceptingPatients = false, Rating = 4.8, ReviewCount = 203, Latitude = 34.0966, Longitude = -118.3863 }
        };
        context.Set<DoctorProfile>().AddRange(doctors);

        var patientId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var patientId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");

        // Appointments
        var appointments = new[]
        {
            new Appointment { Id = Guid.Parse("00000000-0000-0000-0009-000000000010"), DoctorId = doctors[0].Id, PatientId = patientId1, Type = AppointmentType.InPerson, Status = AppointmentStatus.Completed, ScheduledAt = DateTime.UtcNow.AddDays(-3), DurationMinutes = 30, Notes = "Follow-up after minor injury. Patient recovering well." },
            new Appointment { Id = Guid.Parse("00000000-0000-0000-0009-000000000011"), DoctorId = doctors[1].Id, PatientId = patientId1, Type = AppointmentType.Telehealth, Status = AppointmentStatus.Scheduled, ScheduledAt = DateTime.UtcNow.AddDays(2), DurationMinutes = 20 },
            new Appointment { Id = Guid.Parse("00000000-0000-0000-0009-000000000012"), DoctorId = doctors[1].Id, PatientId = patientId2, Type = AppointmentType.FollowUp, Status = AppointmentStatus.Confirmed, ScheduledAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 15 },
            new Appointment { Id = Guid.Parse("00000000-0000-0000-0009-000000000013"), DoctorId = doctors[2].Id, PatientId = patientId2, Type = AppointmentType.Telehealth, Status = AppointmentStatus.Scheduled, ScheduledAt = DateTime.UtcNow.AddDays(5), DurationMinutes = 30 },
            new Appointment { Id = Guid.Parse("00000000-0000-0000-0009-000000000014"), DoctorId = doctors[0].Id, PatientId = patientId2, Type = AppointmentType.InPerson, Status = AppointmentStatus.Cancelled, ScheduledAt = DateTime.UtcNow.AddDays(-1), DurationMinutes = 45, Notes = "Cancelled by patient - rescheduled to telehealth" }
        };
        context.Set<Appointment>().AddRange(appointments);

        await context.SaveChangesAsync(ct);
    }
}
