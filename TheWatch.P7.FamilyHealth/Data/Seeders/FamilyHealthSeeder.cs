using Microsoft.EntityFrameworkCore;
using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Data.Seeders;

public class FamilyHealthSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(FamilyHealthDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<FamilyGroup>().AnyAsync(ct))
            return;

        // Families
        var families = new[]
        {
            new FamilyGroup { Id = Guid.Parse("00000000-0000-0000-0007-000000000001"), Name = "The Test Family" },
            new FamilyGroup { Id = Guid.Parse("00000000-0000-0000-0007-000000000002"), Name = "The Smith-Johnson Household" }
        };
        context.Set<FamilyGroup>().AddRange(families);

        // Members
        var members = new[]
        {
            new FamilyMember { Id = Guid.Parse("00000000-0000-0000-0007-000000000010"), FamilyGroupId = families[0].Id, Name = "Alice Test", Role = FamilyRole.Parent, Phone = "+15551000001", Email = "alice@test.com" },
            new FamilyMember { Id = Guid.Parse("00000000-0000-0000-0007-000000000011"), FamilyGroupId = families[0].Id, Name = "Bob Test", Role = FamilyRole.Parent, Phone = "+15551000002", Email = "bob@test.com" },
            new FamilyMember { Id = Guid.Parse("00000000-0000-0000-0007-000000000012"), FamilyGroupId = families[1].Id, Name = "Carol Smith", Role = FamilyRole.Guardian, Phone = "+15551000003", Email = "carol@test.com" },
            new FamilyMember { Id = Guid.Parse("00000000-0000-0000-0007-000000000013"), FamilyGroupId = families[1].Id, Name = "Grandma Rose", Role = FamilyRole.ElderlyRelative, Phone = "+15551000004" }
        };
        context.Set<FamilyMember>().AddRange(members);

        // Vital Readings (10 readings)
        var rng = new Random(42);
        var vitals = new List<VitalReading>();
        for (int i = 0; i < 10; i++)
        {
            var member = members[i % 4];
            var vitalType = (VitalType)(i % 6);
            double value = vitalType switch
            {
                VitalType.HeartRate => 60 + rng.Next(40),
                VitalType.Temperature => 36.0 + rng.NextDouble() * 2.0,
                VitalType.SpO2 => 94 + rng.Next(6),
                VitalType.RespiratoryRate => 12 + rng.Next(10),
                VitalType.BloodGlucose => 70 + rng.Next(60),
                _ => 120
            };
            vitals.Add(new VitalReading
            {
                Id = Guid.Parse($"00000000-0000-0000-0007-000000001{i:D3}"),
                MemberId = member.Id,
                Type = vitalType,
                Value = Math.Round(value, 1),
                Timestamp = DateTime.UtcNow.AddHours(-i * 2)
            });
        }
        context.Set<VitalReading>().AddRange(vitals);

        await context.SaveChangesAsync(ct);
    }
}
