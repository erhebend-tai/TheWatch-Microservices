using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.FamilyHealth.Models;

namespace TheWatch.Contracts.FamilyHealth;

public class FamilyHealthClient(HttpClient http) : ServiceClientBase(http, "FamilyHealth"), IFamilyHealthClient
{
    public Task<FamilyGroupDto> CreateGroupAsync(CreateFamilyGroupRequest request, CancellationToken ct)
        => PostAsync<FamilyGroupDto>("/api/families", request, ct);

    public Task<FamilyGroupResponse> GetGroupAsync(Guid groupId, CancellationToken ct)
        => GetAsync<FamilyGroupResponse>($"/api/families/{groupId}", ct);

    public Task<List<FamilyGroupDto>> ListGroupsAsync(CancellationToken ct)
        => GetAsync<List<FamilyGroupDto>>("/api/families", ct);

    public Task<FamilyMemberDto> AddMemberAsync(Guid groupId, AddMemberRequest request, CancellationToken ct)
        => PostAsync<FamilyMemberDto>($"/api/families/{groupId}/members", request, ct);

    public Task RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct)
        => DeleteAsync($"/api/families/{groupId}/members/{memberId}", ct);

    public Task<CheckInDto> CheckInAsync(Guid memberId, CreateCheckInRequest request, CancellationToken ct)
        => PostAsync<CheckInDto>($"/api/members/{memberId}/checkins", request, ct);

    public Task<MemberCheckInHistory> GetCheckInHistoryAsync(Guid memberId, CancellationToken ct)
        => GetAsync<MemberCheckInHistory>($"/api/members/{memberId}/checkins", ct);

    public Task<VitalReadingDto> RecordVitalAsync(Guid memberId, RecordVitalRequest request, CancellationToken ct)
        => PostAsync<VitalReadingDto>($"/api/members/{memberId}/vitals", request, ct);

    public Task<VitalHistory> GetVitalHistoryAsync(Guid memberId, VitalType? type, CancellationToken ct)
    {
        var query = $"/api/members/{memberId}/vitals";
        if (type.HasValue) query += $"?type={type.Value}";
        return GetAsync<VitalHistory>(query, ct);
    }

    public Task<List<MedicalAlertDto>> GetAlertsAsync(Guid memberId, CancellationToken ct)
        => GetAsync<List<MedicalAlertDto>>($"/api/members/{memberId}/alerts", ct);

    public Task AcknowledgeAlertAsync(Guid alertId, CancellationToken ct)
        => PostAsync($"/api/alerts/{alertId}/acknowledge", ct);
}
