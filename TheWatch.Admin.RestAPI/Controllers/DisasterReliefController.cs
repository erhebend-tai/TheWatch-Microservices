using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.DisasterRelief;
using TheWatch.Contracts.DisasterRelief.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/disaster")]
[Authorize(Policy = "AdminOnly")]
public class DisasterReliefController(IDisasterReliefClient disaster) : ControllerBase
{
    [HttpGet("events")]
    public async Task<IActionResult> ListEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] EventStatus? status = null, CancellationToken ct = default)
        => Ok(await disaster.ListEventsAsync(page, pageSize, status, ct));

    [HttpGet("events/{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id, CancellationToken ct)
        => Ok(await disaster.GetEventAsync(id, ct));

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateDisasterEventRequest request, CancellationToken ct)
        => Ok(await disaster.CreateEventAsync(request, ct));

    [HttpPut("events/{id:guid}/status")]
    public async Task<IActionResult> UpdateEventStatus(Guid id, [FromBody] UpdateEventStatusRequest request, CancellationToken ct)
        => Ok(await disaster.UpdateEventStatusAsync(id, request, ct));

    [HttpGet("shelters")]
    public async Task<IActionResult> ListShelters([FromQuery] Guid? eventId = null, CancellationToken ct = default)
        => Ok(await disaster.ListSheltersAsync(eventId, ct));

    [HttpGet("shelters/{id:guid}")]
    public async Task<IActionResult> GetShelter(Guid id, CancellationToken ct)
        => Ok(await disaster.GetShelterAsync(id, ct));

    [HttpPost("shelters")]
    public async Task<IActionResult> CreateShelter([FromBody] CreateShelterRequest request, CancellationToken ct)
        => Ok(await disaster.CreateShelterAsync(request, ct));

    [HttpPut("shelters/{id:guid}/occupancy")]
    public async Task<IActionResult> UpdateOccupancy(Guid id, [FromBody] UpdateOccupancyRequest request, CancellationToken ct)
    {
        await disaster.UpdateOccupancyAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("resources/donate")]
    public async Task<IActionResult> DonateResource([FromBody] DonateResourceRequest request, CancellationToken ct)
        => Ok(await disaster.DonateResourceAsync(request, ct));

    [HttpPost("resources/request")]
    public async Task<IActionResult> CreateResourceRequest([FromBody] CreateResourceRequestRecord request, CancellationToken ct)
        => Ok(await disaster.CreateResourceRequestAsync(request, ct));

    [HttpPost("evacuation-routes")]
    public async Task<IActionResult> CreateEvacRoute([FromBody] CreateEvacuationRouteRequest request, CancellationToken ct)
        => Ok(await disaster.CreateEvacRouteAsync(request, ct));

    [HttpGet("evacuation-routes")]
    public async Task<IActionResult> ListEvacRoutes([FromQuery] Guid eventId, CancellationToken ct)
        => Ok(await disaster.ListEvacRoutesAsync(eventId, ct));
}
