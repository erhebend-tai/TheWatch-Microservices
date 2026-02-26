using Microsoft.EntityFrameworkCore;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Data.Seeders;

public class VoiceEmergencySeeder : IWatchDataSeeder
{
    public async Task SeedAsync(VoiceEmergencyDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<Incident>().AnyAsync(ct))
            return;

        var userId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var userId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");
        var userId3 = Guid.Parse("00000000-0000-0000-0000-000000010003");

        // Voice Triggers
        var triggers = new[]
        {
            new VoiceTrigger { Id = Guid.Parse("00000000-0000-0000-0002-000000000001"), UserId = userId1, TriggerPhrase = "help me", TriggerType = TriggerType.Panic, ResponseAction = ResponseAction.AlertEmergency, IsActive = true, IsSystemDefault = true, PriorityLevel = 1 },
            new VoiceTrigger { Id = Guid.Parse("00000000-0000-0000-0002-000000000002"), UserId = userId1, TriggerPhrase = "call 911", TriggerType = TriggerType.Panic, ResponseAction = ResponseAction.Call911, IsActive = true, IsSystemDefault = true, PriorityLevel = 1 },
            new VoiceTrigger { Id = Guid.Parse("00000000-0000-0000-0002-000000000003"), UserId = userId2, TriggerPhrase = "chlorine gas", TriggerType = TriggerType.Chemical, ResponseAction = ResponseAction.ChemicalLookup, IsActive = true, PriorityLevel = 2 },
            new VoiceTrigger { Id = Guid.Parse("00000000-0000-0000-0002-000000000004"), UserId = userId2, TriggerPhrase = "I need help", TriggerType = TriggerType.Medical, ResponseAction = ResponseAction.AlertContacts, IsActive = true, PriorityLevel = 3 },
            new VoiceTrigger { Id = Guid.Parse("00000000-0000-0000-0002-000000000005"), UserId = userId3, TriggerPhrase = "everything is fine", TriggerType = TriggerType.Duress, ResponseAction = ResponseAction.AlertEmergency, IsActive = true, PriorityLevel = 1 }
        };
        context.Set<VoiceTrigger>().AddRange(triggers);

        // Incidents
        var incidents = new[]
        {
            new Incident { Id = Guid.Parse("00000000-0000-0000-0002-000000000010"), Type = EmergencyType.MedicalEmergency, Description = "Voice trigger detected: 'help me' with high confidence", Status = IncidentStatus.Resolved, ReporterId = userId1, ReporterName = "Alice Test", Severity = 4, Location = new Location(34.0522, -118.2437), ResolvedAt = DateTime.UtcNow.AddHours(-2) },
            new Incident { Id = Guid.Parse("00000000-0000-0000-0002-000000000011"), Type = EmergencyType.ChemicalHazard, Description = "Chemical exposure detected: chlorine gas in industrial area", Status = IncidentStatus.InProgress, ReporterId = userId2, ReporterName = "Bob Test", Severity = 5, Location = new Location(34.0525, -118.2440) },
            new Incident { Id = Guid.Parse("00000000-0000-0000-0002-000000000012"), Type = EmergencyType.Other, Description = "Welfare check escalation - missed trip check-in", Status = IncidentStatus.Dispatched, ReporterId = userId3, Severity = 3, Location = new Location(34.0530, -118.2450) }
        };
        context.Set<Incident>().AddRange(incidents);

        // Dispatches
        var dispatches = new[]
        {
            new Dispatch { Id = Guid.Parse("00000000-0000-0000-0002-000000000020"), IncidentId = incidents[0].Id, Status = DispatchStatus.Completed, RadiusKm = 5.0, RespondersRequested = 3, AcknowledgedAt = DateTime.UtcNow.AddHours(-3) },
            new Dispatch { Id = Guid.Parse("00000000-0000-0000-0002-000000000021"), IncidentId = incidents[1].Id, Status = DispatchStatus.EnRoute, RadiusKm = 10.0, RespondersRequested = 5 }
        };
        context.Set<Dispatch>().AddRange(dispatches);

        // Emergency Contacts
        var contacts = new[]
        {
            new EmergencyContact { Id = Guid.Parse("00000000-0000-0000-0002-000000000030"), UserId = userId1, ContactName = "John Test (Spouse)", Phone = "+15551234567", Email = "john@test.com", Relationship = "Spouse", Priority = 1, ReceiveAlerts = true, ReceiveTripUpdates = true, CanCancelEmergency = true },
            new EmergencyContact { Id = Guid.Parse("00000000-0000-0000-0002-000000000031"), UserId = userId2, ContactName = "Safety Officer", Phone = "+15559876543", Relationship = "Workplace Safety", Priority = 1, ReceiveAlerts = true }
        };
        context.Set<EmergencyContact>().AddRange(contacts);

        await context.SaveChangesAsync(ct);
    }
}
