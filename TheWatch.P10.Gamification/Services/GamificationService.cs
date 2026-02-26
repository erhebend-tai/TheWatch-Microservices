using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, UserReward> _rewards = new();
    private readonly ConcurrentDictionary<Guid, Challenge> _challenges = new();
    private readonly ConcurrentDictionary<Guid, string> _displayNames = new();

    public Task<UserReward> GetOrCreateRewardAsync(Guid userId, string displayName)
    {
        _displayNames[userId] = displayName;
        var reward = _rewards.GetOrAdd(userId, _ => new UserReward { UserId = userId });
        return Task.FromResult(reward);
    }

    public Task<UserReward> AwardPointsAsync(AwardPointsRequest request)
    {
        var reward = _rewards.GetOrAdd(request.UserId, _ => new UserReward { UserId = request.UserId });
        reward.TotalPoints += request.Points;
        reward.Level = 1 + reward.TotalPoints / 1000; // Level up every 1000 points
        reward.LastActivityAt = DateTime.UtcNow;

        return Task.FromResult(reward);
    }

    public Task<UserReward> AwardBadgeAsync(AwardBadgeRequest request)
    {
        var reward = _rewards.GetOrAdd(request.UserId, _ => new UserReward { UserId = request.UserId });
        if (!reward.Badges.Contains(request.Badge))
            reward.Badges.Add(request.Badge);
        reward.LastActivityAt = DateTime.UtcNow;

        return Task.FromResult(reward);
    }

    public Task<Challenge> CreateChallengeAsync(CreateChallengeRequest request)
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

        _challenges[challenge.Id] = challenge;
        return Task.FromResult(challenge);
    }

    public Task<List<Challenge>> ListChallengesAsync(ChallengeStatus? status)
    {
        var query = _challenges.Values.AsEnumerable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return Task.FromResult(query.OrderByDescending(c => c.CreatedAt).ToList());
    }

    public Task<LeaderboardResponse> GetLeaderboardAsync(int top)
    {
        var entries = _rewards.Values
            .OrderByDescending(r => r.TotalPoints)
            .Take(top)
            .Select((r, idx) => new LeaderboardEntry
            {
                UserId = r.UserId,
                DisplayName = _displayNames.GetValueOrDefault(r.UserId, "User"),
                Points = r.TotalPoints,
                Level = r.Level,
                Rank = idx + 1
            })
            .ToList();

        return Task.FromResult(new LeaderboardResponse(entries, _rewards.Count));
    }

    public Task ExpireChallengesAsync()
    {
        var now = DateTime.UtcNow;
        foreach (var c in _challenges.Values.Where(c => c.Status == ChallengeStatus.Active && c.ExpiresAt.HasValue && c.ExpiresAt < now))
        {
            c.Status = ChallengeStatus.Expired;
        }
        return Task.CompletedTask;
    }
}
