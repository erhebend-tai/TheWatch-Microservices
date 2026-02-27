namespace TheWatch.Contracts.Gamification.Models;

public record AwardPointsRequest(Guid UserId, int Points, string Reason);
public record AwardBadgeRequest(Guid UserId, string Badge);
public record CreateChallengeRequest(string Name, string? Description, ChallengeType Type, int TargetValue, int PointsReward, string? BadgeReward = null, DateTime? ExpiresAt = null);
