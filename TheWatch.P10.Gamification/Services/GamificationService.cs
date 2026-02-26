using Microsoft.EntityFrameworkCore;
using TheWatch.P10.Gamification.Gaming;

namespace TheWatch.P10.Gamification.Services;

public interface IGamificationService
{
    Task<UserReward> GetOrCreateRewardAsync(Guid userId, string displayName = "User");
    Task<UserReward> AwardPointsAsync(AwardPointsRequest request);
    Task<UserReward> AwardBadgeAsync(AwardBadgeRequest request);
    Task<Challenge> CreateChallengeAsync(CreateChallengeRequest request);
    Task<List<Challenge>> ListChallengesAsync(ChallengeStatus? status = null);
    Task<LeaderboardResponse> GetLeaderboardAsync(int top = 50);
    Task ExpireChallengesAsync();
}

public class GamificationService : IGamificationService
{
    private readonly IWatchRepository<UserReward> _rewards;
    private readonly IWatchRepository<Challenge> _challenges;

    public GamificationService(IWatchRepository<UserReward> rewards, IWatchRepository<Challenge> challenges)
    {
        _rewards = rewards;
        _challenges = challenges;
    }

    public async Task<UserReward> GetOrCreateRewardAsync(Guid userId, string displayName)
    {
        var existing = await _rewards.Query()
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (existing is not null) return existing;

        var reward = new UserReward { UserId = userId };
        return await _rewards.AddAsync(reward);
    }

    public async Task<UserReward> AwardPointsAsync(AwardPointsRequest request)
    {
        var reward = await _rewards.Query()
            .FirstOrDefaultAsync(r => r.UserId == request.UserId);

        if (reward is null)
        {
            reward = new UserReward { UserId = request.UserId };
            await _rewards.AddAsync(reward);
        }

        reward.TotalPoints += request.Points;
        reward.Level = 1 + reward.TotalPoints / 1000; // Level up every 1000 points
        reward.LastActivityAt = DateTime.UtcNow;
        await _rewards.UpdateAsync(reward);

        return reward;
    }

    public async Task<UserReward> AwardBadgeAsync(AwardBadgeRequest request)
    {
        var reward = await _rewards.Query()
            .FirstOrDefaultAsync(r => r.UserId == request.UserId);

        if (reward is null)
        {
            reward = new UserReward { UserId = request.UserId };
            await _rewards.AddAsync(reward);
        }

        if (!reward.Badges.Contains(request.Badge))
            reward.Badges.Add(request.Badge);
        reward.LastActivityAt = DateTime.UtcNow;
        await _rewards.UpdateAsync(reward);

        return reward;
    }

    public async Task<Challenge> CreateChallengeAsync(CreateChallengeRequest request)
    {
        var challenge = new Challenge
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            TargetValue = request.TargetValue,
            PointsReward = request.PointsReward,
            BadgeReward = request.BadgeReward,
            ExpiresAt = request.ExpiresAt
        };

        return await _challenges.AddAsync(challenge);
    }

    public async Task<List<Challenge>> ListChallengesAsync(ChallengeStatus? status)
    {
        var query = _challenges.Query();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync(int top)
    {
        var totalPlayers = await _rewards.Query().CountAsync();

        var entries = await _rewards.Query()
            .OrderByDescending(r => r.TotalPoints)
            .Take(top)
            .ToListAsync();

        var leaderboard = entries.Select((r, idx) => new LeaderboardEntry
        {
            UserId = r.UserId,
            DisplayName = "User", // DisplayName not stored on UserReward; use UserProfile from P1
            Points = r.TotalPoints,
            Level = r.Level,
            Rank = idx + 1
        }).ToList();

        return new LeaderboardResponse(leaderboard, totalPlayers);
    }

    public async Task ExpireChallengesAsync()
    {
        var now = DateTime.UtcNow;
        var expiring = await _challenges.Query()
            .Where(c => c.Status == ChallengeStatus.Active && c.ExpiresAt.HasValue && c.ExpiresAt < now)
            .ToListAsync();

        foreach (var c in expiring)
        {
            c.Status = ChallengeStatus.Expired;
            await _challenges.UpdateAsync(c);
        }
    }
}
