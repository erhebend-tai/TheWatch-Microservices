using TheWatch.Contracts.Gamification.Models;

namespace TheWatch.Contracts.Gamification;

public interface IGamificationClient
{
    Task<UserRewardDto> GetUserRewardAsync(Guid userId, CancellationToken ct = default);
    Task<UserRewardResponse> GetUserRewardWithChallengesAsync(Guid userId, CancellationToken ct = default);
    Task<UserRewardDto> AwardPointsAsync(AwardPointsRequest request, CancellationToken ct = default);
    Task<UserRewardDto> AwardBadgeAsync(AwardBadgeRequest request, CancellationToken ct = default);
    Task<ChallengeDto> CreateChallengeAsync(CreateChallengeRequest request, CancellationToken ct = default);
    Task<List<ChallengeDto>> ListChallengesAsync(ChallengeStatus? status = null, CancellationToken ct = default);
    Task<LeaderboardResponse> GetLeaderboardAsync(int top = 50, CancellationToken ct = default);
}
