namespace TheWatch.Contracts.Gamification.Models;

public class UserRewardDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TotalPoints { get; set; }
    public int Level { get; set; }
    public List<string> Badges { get; set; } = [];
    public int StreakDays { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChallengeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChallengeType Type { get; set; }
    public int TargetValue { get; set; }
    public int PointsReward { get; set; }
    public string? BadgeReward { get; set; }
    public ChallengeStatus Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeaderboardEntryDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Level { get; set; }
    public int Rank { get; set; }
}
