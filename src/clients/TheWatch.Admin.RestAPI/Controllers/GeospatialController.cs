using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.Geospatial;
using TheWatch.Contracts.Geospatial.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/geo")]
[Authorize(Policy = "AdminOnly")]
public class GeospatialController(IGeospatialClient geo) : ControllerBase
{
    [HttpGet("responders/nearby")]
    public async Task<IActionResult> FindNearestResponders([FromQuery] double lon, [FromQuery] double lat, [FromQuery] int count = 10, [FromQuery] double maxRadius = 10000, CancellationToken ct = default)
        => Ok(await geo.FindNearestRespondersAsync(lon, lat, count, maxRadius, ct));

    [HttpGet("shelters/nearby")]
    public async Task<IActionResult> FindNearestShelters([FromQuery] double lon, [FromQuery] double lat, [FromQuery] int count = 10, [FromQuery] double maxRadius = 50000, CancellationToken ct = default)
        => Ok(await geo.FindNearestSheltersAsync(lon, lat, count, maxRadius, ct));

    [HttpGet("pois/nearby")]
    public async Task<IActionResult> FindNearestPois([FromQuery] double lon, [FromQuery] double lat, [FromQuery] int count = 10, [FromQuery] string? category = null, [FromQuery] double maxRadius = 5000, CancellationToken ct = default)
        => Ok(await geo.FindNearestPoisAsync(lon, lat, count, category, maxRadius, ct));

    [HttpPost("zones/incident")]
    public async Task<IActionResult> CreateIncidentZone([FromBody] CreateIncidentZoneRequest request, CancellationToken ct)
        => Ok(await geo.CreateIncidentZoneAsync(request, ct));

    [HttpPut("zones/{zoneId:guid}/expand")]
    public async Task<IActionResult> ExpandZone(Guid zoneId, [FromBody] ExpandZoneRequest request, CancellationToken ct)
        => Ok(await geo.ExpandIncidentZoneAsync(zoneId, request, ct));

    [HttpGet("zones/{zoneId:guid}/contains")]
    public async Task<IActionResult> IsPointInZone(Guid zoneId, [FromQuery] double lon, [FromQuery] double lat, CancellationToken ct)
        => Ok(await geo.IsPointInZoneAsync(lon, lat, zoneId, ct));

    [HttpPost("geofences")]
    public async Task<IActionResult> CreateGeofence([FromBody] CreateGeofenceRequest request, CancellationToken ct)
        => Ok(await geo.CreateGeofenceAsync(request, ct));

    [HttpPost("tracking")]
    public async Task<IActionResult> RegisterTrackedEntity([FromBody] RegisterTrackedEntityRequest request, CancellationToken ct)
        => Ok(await geo.RegisterTrackedEntityAsync(request, ct));

    [HttpPut("tracking/{entityId:guid}/location")]
    public async Task<IActionResult> UpdateEntityLocation(Guid entityId, [FromBody] UpdateEntityLocationRequest request, CancellationToken ct)
        => Ok(await geo.UpdateEntityLocationAsync(entityId, request, ct));

    [HttpPost("geofences/check")]
    public async Task<IActionResult> CheckGeofences([FromBody] CheckGeofencesRequest request, CancellationToken ct)
        => Ok(await geo.CheckGeofencesAsync(request, ct));
}
