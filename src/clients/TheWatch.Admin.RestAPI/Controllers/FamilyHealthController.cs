using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.FamilyHealth;
using TheWatch.Contracts.FamilyHealth.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/family")]
[Authorize(Policy = "AdminOnly")]
public class FamilyHealthController(IFamilyHealthClient family) : ControllerBase
{
    [HttpGet("groups")]
    public async Task<IActionResult> ListGroups(CancellationToken ct)
        => Ok(await family.ListGroupsAsync(ct));

    [HttpGet("groups/{groupId:guid}")]
    public async Task<IActionResult> GetGroup(Guid groupId, CancellationToken ct)
        => Ok(await family.GetGroupAsync(groupId, ct));

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateFamilyGroupRequest request, CancellationToken ct)
        => Ok(await family.CreateGroupAsync(request, ct));

    [HttpPost("groups/{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddMemberRequest request, CancellationToken ct)
        => Ok(await family.AddMemberAsync(groupId, request, ct));

    [HttpDelete("groups/{groupId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId, CancellationToken ct)
    {
        await family.RemoveMemberAsync(groupId, memberId, ct);
        return NoContent();
    }

    [HttpPost("members/{memberId:guid}/checkins")]
    public async Task<IActionResult> CheckIn(Guid memberId, [FromBody] CreateCheckInRequest request, CancellationToken ct)
        => Ok(await family.CheckInAsync(memberId, request, ct));

    [HttpGet("members/{memberId:guid}/checkins")]
    public async Task<IActionResult> GetCheckInHistory(Guid memberId, CancellationToken ct)
        => Ok(await family.GetCheckInHistoryAsync(memberId, ct));

    [HttpPost("members/{memberId:guid}/vitals")]
    public async Task<IActionResult> RecordVital(Guid memberId, [FromBody] RecordVitalRequest request, CancellationToken ct)
        => Ok(await family.RecordVitalAsync(memberId, request, ct));

    [HttpGet("members/{memberId:guid}/vitals")]
    public async Task<IActionResult> GetVitalHistory(Guid memberId, [FromQuery] VitalType? type = null, CancellationToken ct = default)
        => Ok(await family.GetVitalHistoryAsync(memberId, type, ct));

    [HttpGet("members/{memberId:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid memberId, CancellationToken ct)
        => Ok(await family.GetAlertsAsync(memberId, ct));

    [HttpPost("alerts/{alertId:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId, CancellationToken ct)
    {
        await family.AcknowledgeAlertAsync(alertId, ct);
        return NoContent();
    }
}
