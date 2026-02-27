using TheWatch.Contracts.FamilyHealth.Models;

namespace TheWatch.Contracts.FamilyHealth;

public interface IFamilyHealthClient
{
    Task<FamilyGroupDto> CreateGroupAsync(CreateFamilyGroupRequest request, CancellationToken ct = default);
    Task<FamilyGroupResponse> GetGroupAsync(Guid groupId, CancellationToken ct = default);
    Task<List<FamilyGroupDto>> ListGroupsAsync(CancellationToken ct = default);
    Task<FamilyMemberDto> AddMemberAsync(Guid groupId, AddMemberRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct = default);
    Task<CheckInDto> CheckInAsync(Guid memberId, CreateCheckInRequest request, CancellationToken ct = default);
    Task<MemberCheckInHistory> GetCheckInHistoryAsync(Guid memberId, CancellationToken ct = default);
    Task<VitalReadingDto> RecordVitalAsync(Guid memberId, RecordVitalRequest request, CancellationToken ct = default);
    Task<VitalHistory> GetVitalHistoryAsync(Guid memberId, VitalType? type = null, CancellationToken ct = default);
    Task<List<MedicalAlertDto>> GetAlertsAsync(Guid memberId, CancellationToken ct = default);
    Task AcknowledgeAlertAsync(Guid alertId, CancellationToken ct = default);
}
