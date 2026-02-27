using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.Gamification;
using TheWatch.Contracts.Gamification.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/gamification")]
[Authorize(Policy = "AdminOnly")]
public class GamificationController(IGamificationClient gamification) : ControllerBase
{
    [HttpGet("rewards/{userId:guid}")]
    public async Task<IActionResult> GetUserReward(Guid userId, CancellationToken ct)
        => Ok(await gamification.GetUserRewardAsync(userId, ct));

    [HttpGet("rewards/{userId:guid}/full")]
    public async Task<IActionResult> GetUserRewardWithChallenges(Guid userId, CancellationToken ct)
        => Ok(await gamification.GetUserRewardWithChallengesAsync(userId, ct));

    [HttpPost("rewards/points")]
    public async Task<IActionResult> AwardPoints([FromBody] AwardPointsRequest request, CancellationToken ct)
        => Ok(await gamification.AwardPointsAsync(request, ct));

    [HttpPost("rewards/badges")]
    public async Task<IActionResult> AwardBadge([FromBody] AwardBadgeRequest request, CancellationToken ct)
        => Ok(await gamification.AwardBadgeAsync(request, ct));

    [HttpPost("challenges")]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request, CancellationToken ct)
        => Ok(await gamification.CreateChallengeAsync(request, ct));

    [HttpGet("challenges")]
    public async Task<IActionResult> ListChallenges([FromQuery] ChallengeStatus? status = null, CancellationToken ct = default)
        => Ok(await gamification.ListChallengesAsync(status, ct));

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 50, CancellationToken ct = default)
        => Ok(await gamification.GetLeaderboardAsync(top, ct));
}
