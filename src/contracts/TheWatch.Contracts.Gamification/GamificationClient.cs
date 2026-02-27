using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Gamification.Models;

namespace TheWatch.Contracts.Gamification;

public class GamificationClient(HttpClient http) : ServiceClientBase(http, "Gamification"), IGamificationClient
{
    public Task<UserRewardDto> GetUserRewardAsync(Guid userId, CancellationToken ct)
        => GetAsync<UserRewardDto>($"/api/rewards/{userId}", ct);

    public Task<UserRewardResponse> GetUserRewardWithChallengesAsync(Guid userId, CancellationToken ct)
        => GetAsync<UserRewardResponse>($"/api/rewards/{userId}/full", ct);

    public Task<UserRewardDto> AwardPointsAsync(AwardPointsRequest request, CancellationToken ct)
        => PostAsync<UserRewardDto>("/api/rewards/points", request, ct);

    public Task<UserRewardDto> AwardBadgeAsync(AwardBadgeRequest request, CancellationToken ct)
        => PostAsync<UserRewardDto>("/api/rewards/badges", request, ct);

    public Task<ChallengeDto> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken ct)
        => PostAsync<ChallengeDto>("/api/challenges", request, ct);

    public Task<List<ChallengeDto>> ListChallengesAsync(ChallengeStatus? status, CancellationToken ct)
    {
        var query = status.HasValue ? $"/api/challenges?status={status.Value}" : "/api/challenges";
        return GetAsync<List<ChallengeDto>>(query, ct);
    }

    public Task<LeaderboardResponse> GetLeaderboardAsync(int top, CancellationToken ct)
        => GetAsync<LeaderboardResponse>($"/api/leaderboard?top={top}", ct);
}
