using Microsoft.EntityFrameworkCore;
using TheWatch.P1.CoreGateway.Core;
using TheWatch.P1.CoreGateway.Measurements;

namespace TheWatch.P1.CoreGateway.Data.Seeders;

public class CoreGatewaySeeder : IWatchDataSeeder
{
    public async Task SeedAsync(CoreGatewayDbContext context, CancellationToken ct = default)
    {
        // Seed test users
        if (!await context.Set<UserProfile>().AnyAsync(ct))
        {
            var users = new[]
            {
                new UserProfile { Id = Guid.Parse("00000000-0000-0000-0000-000000010001"), DisplayName = "Alice Test", Email = "alice@thewatch.test", Phone = "+15551000001", Role = UserRole.Citizen, Latitude = 34.0522, Longitude = -118.2437, IsActive = true },
                new UserProfile { Id = Guid.Parse("00000000-0000-0000-0000-000000010002"), DisplayName = "Bob Test", Email = "bob@thewatch.test", Phone = "+15551000002", Role = UserRole.Responder, Latitude = 34.0530, Longitude = -118.2450, IsActive = true },
                new UserProfile { Id = Guid.Parse("00000000-0000-0000-0000-000000010003"), DisplayName = "Charlie Test", Email = "charlie@thewatch.test", Phone = "+15551000003", Role = UserRole.Citizen, Latitude = 34.0515, Longitude = -118.2420, IsActive = true },
                new UserProfile { Id = Guid.Parse("00000000-0000-0000-0000-000000010004"), DisplayName = "Diana Admin", Email = "diana@thewatch.test", Phone = "+15551000004", Role = UserRole.Admin, IsActive = true },
                new UserProfile { Id = Guid.Parse("00000000-0000-0000-0000-000000010005"), DisplayName = "System Operator", Email = "sysop@thewatch.test", Role = UserRole.SystemOperator, IsActive = true }
            };
            context.Set<UserProfile>().AddRange(users);
            await context.SaveChangesAsync(ct);
        }

        // Seed measurement reference data
        await MeasurementSeeder.SeedAsync(context, ct);
    }
}
