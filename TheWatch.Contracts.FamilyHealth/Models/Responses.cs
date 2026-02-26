namespace TheWatch.Contracts.FamilyHealth.Models;

public record FamilyGroupResponse(FamilyGroupDto Group, List<FamilyMemberDto> Members);
public record MemberCheckInHistory(FamilyMemberDto Member, List<CheckInDto> CheckIns);
public record VitalHistory(List<VitalReadingDto> Readings, int TotalCount);
