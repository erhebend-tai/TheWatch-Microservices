using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.Notifications;
using TheWatch.Contracts.Notifications.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

/// <summary>
/// Admin proxy for TheWatch P12 Notifications service.
/// Provides notification management, broadcast, and stats endpoints.
/// </summary>
[ApiController]
[Route("api/admin/notifications")]
[Authorize(Policy = "AdminOnly")]
public class NotificationsController(INotificationsClient notifications) : ControllerBase
{
    // === Send & List ===

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken ct)
        => Created(string.Empty, await notifications.SendAsync(request, ct));

    [HttpGet("{recipientId:guid}")]
    public async Task<IActionResult> List(Guid recipientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await notifications.ListAsync(recipientId, page, pageSize, ct));

    [HttpGet("record/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await notifications.GetAsync(id, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    // === Broadcast ===

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request, CancellationToken ct)
        => Created(string.Empty, await notifications.BroadcastAsync(request, ct));

    // === Stats ===

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
        => Ok(await notifications.GetStatsAsync(ct));

    // === Preferences ===

    [HttpPost("preferences")]
    [AllowAnonymous]
    public async Task<IActionResult> SetPreference([FromBody] SetNotificationPreferenceRequest request, CancellationToken ct)
        => Ok(await notifications.SetPreferenceAsync(request, ct));

    [HttpGet("preferences/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPreferences(Guid userId, CancellationToken ct)
        => Ok(await notifications.GetPreferencesAsync(userId, ct));
}
