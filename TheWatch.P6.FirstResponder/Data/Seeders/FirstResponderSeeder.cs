using Microsoft.EntityFrameworkCore;
using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Data.Seeders;

public class FirstResponderSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(FirstResponderDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<Responder>().AnyAsync(ct))
            return;

        // Responders
        var responders = new[]
        {
            new Responder { Id = Guid.Parse("00000000-0000-0000-0006-000000000001"), Name = "Officer Sarah Chen", BadgeNumber = "PD-4521", Type = ResponderType.Police, Status = ResponderStatus.Available, Phone = "+15551001001", Email = "s.chen@watchpd.test", LastKnownLocation = new GeoLocation(34.0522, -118.2437), Certifications = ["Law Enforcement", "First Aid", "Crowd Control"], MaxResponseRadiusKm = 30.0 },
            new Responder { Id = Guid.Parse("00000000-0000-0000-0006-000000000002"), Name = "Paramedic James Rivera", BadgeNumber = "EMS-7789", Type = ResponderType.EMS, Status = ResponderStatus.Available, Phone = "+15551001002", Email = "j.rivera@watchems.test", LastKnownLocation = new GeoLocation(34.0530, -118.2450), Certifications = ["Paramedic-ALS", "ACLS", "PALS"], MaxResponseRadiusKm = 25.0 },
            new Responder { Id = Guid.Parse("00000000-0000-0000-0006-000000000003"), Name = "Firefighter Maria Lopez", BadgeNumber = "FD-3345", Type = ResponderType.Fire, Status = ResponderStatus.EnRoute, Phone = "+15551001003", Email = "m.lopez@watchfd.test", LastKnownLocation = new GeoLocation(34.0515, -118.2420), Certifications = ["Firefighter II", "HazMat Awareness"] },
            new Responder { Id = Guid.Parse("00000000-0000-0000-0006-000000000004"), Name = "SAR Lead Tom Bradley", BadgeNumber = "SAR-0012", Type = ResponderType.SAR, Status = ResponderStatus.OffDuty, Phone = "+15551001004", Email = "t.bradley@watchsar.test", LastKnownLocation = new GeoLocation(34.0540, -118.2460), Certifications = ["SAR Team Lead", "Wilderness First Responder", "Swift Water Rescue"], MaxResponseRadiusKm = 50.0 },
            new Responder { Id = Guid.Parse("00000000-0000-0000-0006-000000000005"), Name = "Volunteer Mike Johnson", Type = ResponderType.CommunityWatch, Status = ResponderStatus.Available, Phone = "+15551001005", Email = "m.johnson@community.test", LastKnownLocation = new GeoLocation(34.0510, -118.2430), Certifications = ["CPR", "Basic First Aid"] }
        };
        context.Set<Responder>().AddRange(responders);

        var incidentId = Guid.Parse("00000000-0000-0000-0002-000000000010");

        // Check-Ins
        var checkIns = new[]
        {
            new CheckIn { Id = Guid.Parse("00000000-0000-0000-0006-000000000010"), ResponderId = responders[0].Id, IncidentId = incidentId, Type = CheckInType.Arrived, Location = new GeoLocation(34.0522, -118.2437), Notes = "On scene, situation stable" },
            new CheckIn { Id = Guid.Parse("00000000-0000-0000-0006-000000000011"), ResponderId = responders[1].Id, IncidentId = incidentId, Type = CheckInType.Update, Location = new GeoLocation(34.0530, -118.2450), Notes = "Patient stabilized, preparing transport" },
            new CheckIn { Id = Guid.Parse("00000000-0000-0000-0006-000000000012"), ResponderId = responders[2].Id, IncidentId = incidentId, Type = CheckInType.NeedBackup, Location = new GeoLocation(34.0515, -118.2420), Notes = "Structure fire, requesting additional units" }
        };
        context.Set<CheckIn>().AddRange(checkIns);

        await context.SaveChangesAsync(ct);
    }
}
