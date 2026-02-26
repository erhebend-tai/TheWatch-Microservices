using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P10.Gamification.Gaming;
using Xunit;

namespace TheWatch.P10.Gamification.Tests;

public class GamificationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GamificationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRewards_CreatesDefault()
    {
        var userId = Guid.NewGuid();
        var response = await _client.GetFromJsonAsync<UserReward>($"/api/rewards/{userId}");

        response!.UserId.Should().Be(userId);
        response.TotalPoints.Should().Be(0);
        response.Level.Should().Be(1);
    }

    [Fact]
    public async Task AwardPoints_IncreasesTotal()
    {
        var userId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/api/rewards/points",
            new AwardPointsRequest(userId, 500, "Emergency drill completed"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reward = await response.Content.ReadFromJsonAsync<UserReward>();
        reward!.TotalPoints.Should().Be(500);
    }

    [Fact]
    public async Task AwardPoints_LevelUp()
    {
        var userId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/rewards/points", new AwardPointsRequest(userId, 1500, "Multi"));

        var reward = await _client.GetFromJsonAsync<UserReward>($"/api/rewards/{userId}");
        reward!.Level.Should().Be(2); // 1500 / 1000 = 1, so level = 2
    }

    [Fact]
    public async Task AwardBadge_AddsBadge()
    {
        var userId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/api/rewards/badges",
            new AwardBadgeRequest(userId, "FirstResponderHero"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reward = await response.Content.ReadFromJsonAsync<UserReward>();
        reward!.Badges.Should().Contain("FirstResponderHero");
    }

    [Fact]
    public async Task CreateChallenge_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/challenges",
            new CreateChallengeRequest("10K Steps", "Walk 10,000 steps", ChallengeType.Steps, 10000, 100, "StepMaster"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var challenge = await response.Content.ReadFromJsonAsync<Challenge>();
        challenge!.Name.Should().Be("10K Steps");
        challenge.PointsReward.Should().Be(100);
    }

    [Fact]
    public async Task ListChallenges_ReturnsAll()
    {
        await _client.PostAsJsonAsync("/api/challenges",
            new CreateChallengeRequest("C1", null, ChallengeType.CheckIns, 5, 50));
        await _client.PostAsJsonAsync("/api/challenges",
            new CreateChallengeRequest("C2", null, ChallengeType.Training, 3, 75));

        var challenges = await _client.GetFromJsonAsync<List<Challenge>>("/api/challenges");
        challenges!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetLeaderboard_RanksUsers()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/rewards/points", new AwardPointsRequest(user1, 1000, "test"));
        await _client.PostAsJsonAsync("/api/rewards/points", new AwardPointsRequest(user2, 500, "test"));

        var leaderboard = await _client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard");
        leaderboard!.Entries.Count.Should().BeGreaterThanOrEqualTo(2);
        leaderboard.Entries.First().Points.Should().BeGreaterThanOrEqualTo(leaderboard.Entries.Last().Points);
    }
}
