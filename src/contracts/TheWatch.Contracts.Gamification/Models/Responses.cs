namespace TheWatch.Contracts.Gamification.Models;

public record LeaderboardResponse(List<LeaderboardEntryDto> Entries, int TotalParticipants);
public record UserRewardResponse(UserRewardDto Reward, List<ChallengeDto> ActiveChallenges);
