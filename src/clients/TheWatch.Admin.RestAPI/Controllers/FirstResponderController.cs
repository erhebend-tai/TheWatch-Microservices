using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.FirstResponder.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/responders")]
[Authorize(Policy = "AdminOnly")]
public class FirstResponderController(IFirstResponderClient responders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListResponders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ResponderType? type = null, CancellationToken ct = default)
        => Ok(await responders.ListRespondersAsync(page, pageSize, type, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetResponder(Guid id, CancellationToken ct)
        => Ok(await responders.GetResponderAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> RegisterResponder([FromBody] RegisterResponderRequest request, CancellationToken ct)
        => Ok(await responders.RegisterResponderAsync(request, ct));

    [HttpPut("{id:guid}/location")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken ct)
    {
        await responders.UpdateLocationAsync(id, request, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        await responders.UpdateStatusAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{responderId:guid}/checkins")]
    public async Task<IActionResult> CreateCheckIn(Guid responderId, [FromBody] CreateCheckInRequest request, CancellationToken ct)
        => Ok(await responders.CreateCheckInAsync(responderId, request, ct));

    [HttpGet("nearby")]
    public async Task<IActionResult> FindNearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radius = 10.0, [FromQuery] ResponderType? type = null, [FromQuery] bool availableOnly = true, CancellationToken ct = default)
        => Ok(await responders.FindNearbyAsync(new NearbyResponderQuery(lat, lon, radius, type, availableOnly), ct));
}
