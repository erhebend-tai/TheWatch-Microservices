using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.VoiceEmergency.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/emergency")]
[Authorize(Policy = "AdminOnly")]
public class VoiceEmergencyController(IVoiceEmergencyClient emergency) : ControllerBase
{
    [HttpGet("incidents")]
    public async Task<IActionResult> ListIncidents([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] IncidentStatus? status = null, CancellationToken ct = default)
        => Ok(await emergency.ListIncidentsAsync(page, pageSize, status, ct));

    [HttpGet("incidents/{id:guid}")]
    public async Task<IActionResult> GetIncident(Guid id, CancellationToken ct)
        => Ok(await emergency.GetIncidentAsync(id, ct));

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken ct)
        => Ok(await emergency.CreateIncidentAsync(request, ct));

    [HttpPut("incidents/{id:guid}/status")]
    public async Task<IActionResult> UpdateIncidentStatus(Guid id, [FromBody] UpdateIncidentStatusRequest request, CancellationToken ct)
        => Ok(await emergency.UpdateIncidentStatusAsync(id, request, ct));

    [HttpGet("dispatches/{id:guid}")]
    public async Task<IActionResult> GetDispatch(Guid id, CancellationToken ct)
        => Ok(await emergency.GetDispatchAsync(id, ct));

    [HttpPost("dispatches")]
    public async Task<IActionResult> CreateDispatch([FromBody] CreateDispatchRequest request, CancellationToken ct)
        => Ok(await emergency.CreateDispatchAsync(request, ct));

    [HttpPost("dispatches/{id:guid}/expand")]
    public async Task<IActionResult> ExpandRadius(Guid id, [FromBody] ExpandRadiusRequest request, CancellationToken ct)
        => Ok(await emergency.ExpandRadiusAsync(id, request, ct));
}
