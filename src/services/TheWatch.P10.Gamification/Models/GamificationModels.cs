namespace TheWatch.P10.Gamification.Gaming;

public enum ChallengeStatus
{
    Active,
    Completed,
    Expired,
    Cancelled
}

public enum ChallengeType
{
    Steps,
    HeartRate,
    CheckIns,
    EmergencyDrills,
    CommunityService,
    Training
}

public class UserReward
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int TotalPoints { get; set; }
    public int Level { get; set; } = 1;
    public List<string> Badges { get; set; } = [];
    public int StreakDays { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Challenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChallengeType Type { get; set; }
    public int TargetValue { get; set; }
    public int PointsReward { get; set; }
    public string? BadgeReward { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LeaderboardEntry
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Level { get; set; }
    public int Rank { get; set; }
}

// Request/response records

public record AwardPointsRequest(
    Guid UserId,
    int Points,
    string Reason);

public record AwardBadgeRequest(
    Guid UserId,
    string Badge);

public record CreateChallengeRequest(
    string Name,
    string? Description,
    ChallengeType Type,
    int TargetValue,
    int PointsReward,
    string? BadgeReward = null,
    DateTime? ExpiresAt = null);

public record LeaderboardResponse(
    List<LeaderboardEntry> Entries,
    int TotalParticipants);

public record UserRewardResponse(
    UserReward Reward,
    List<Challenge> ActiveChallenges);
