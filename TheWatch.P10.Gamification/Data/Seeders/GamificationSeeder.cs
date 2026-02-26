using Microsoft.EntityFrameworkCore;
using TheWatch.P10.Gamification.Gaming;

namespace TheWatch.P10.Gamification.Data.Seeders;

public class GamificationSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(GamificationDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<UserReward>().AnyAsync(ct))
            return;

        var userId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var userId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");
        var userId3 = Guid.Parse("00000000-0000-0000-0000-000000010003");

        // User Rewards
        var rewards = new[]
        {
            new UserReward { Id = Guid.Parse("00000000-0000-0000-0010-000000000001"), UserId = userId1, TotalPoints = 850, Level = 3, Badges = ["First Responder", "Community Guardian", "Early Adopter"], StreakDays = 14, LastActivityAt = DateTime.UtcNow.AddHours(-2) },
            new UserReward { Id = Guid.Parse("00000000-0000-0000-0010-000000000002"), UserId = userId2, TotalPoints = 1250, Level = 5, Badges = ["Health Champion", "Safety Star", "Drill Master", "Mesh Pioneer"], StreakDays = 30, LastActivityAt = DateTime.UtcNow.AddHours(-1) },
            new UserReward { Id = Guid.Parse("00000000-0000-0000-0010-000000000003"), UserId = userId3, TotalPoints = 400, Level = 2, Badges = ["Mesh Pioneer", "First Steps"], StreakDays = 5, LastActivityAt = DateTime.UtcNow.AddDays(-1) }
        };
        context.Set<UserReward>().AddRange(rewards);

        // Challenges
        var challenges = new[]
        {
            new Challenge { Id = Guid.Parse("00000000-0000-0000-0010-000000000010"), Name = "10K Steps Challenge", Description = "Walk 10,000 steps per day for 7 consecutive days", Type = ChallengeType.Steps, Status = ChallengeStatus.Active, TargetValue = 70000, PointsReward = 300, BadgeReward = "Step Master", ExpiresAt = DateTime.UtcNow.AddDays(4) },
            new Challenge { Id = Guid.Parse("00000000-0000-0000-0010-000000000011"), Name = "Emergency Drill Week", Description = "Complete 3 emergency response drills this week", Type = ChallengeType.EmergencyDrills, Status = ChallengeStatus.Active, TargetValue = 3, PointsReward = 500, BadgeReward = "Drill Sergeant", ExpiresAt = DateTime.UtcNow.AddDays(6) },
            new Challenge { Id = Guid.Parse("00000000-0000-0000-0010-000000000012"), Name = "Community Service Sprint", Description = "Log 5 hours of community service activities", Type = ChallengeType.CommunityService, Status = ChallengeStatus.Active, TargetValue = 5, PointsReward = 400, ExpiresAt = DateTime.UtcNow.AddDays(14) },
            new Challenge { Id = Guid.Parse("00000000-0000-0000-0010-000000000013"), Name = "Heart Health Week", Description = "Record heart rate readings daily for 7 days", Type = ChallengeType.HeartRate, Status = ChallengeStatus.Expired, TargetValue = 7, PointsReward = 200, BadgeReward = "Heart Watcher" },
            new Challenge { Id = Guid.Parse("00000000-0000-0000-0010-000000000014"), Name = "Check-In Champion", Description = "Complete 20 daily check-ins", Type = ChallengeType.CheckIns, Status = ChallengeStatus.Active, TargetValue = 20, PointsReward = 350, BadgeReward = "Consistent Guardian", ExpiresAt = DateTime.UtcNow.AddDays(30) }
        };
        context.Set<Challenge>().AddRange(challenges);

        await context.SaveChangesAsync(ct);
    }
}
