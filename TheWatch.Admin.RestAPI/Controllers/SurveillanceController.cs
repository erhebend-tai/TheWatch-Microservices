// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.Surveillance;
using TheWatch.Contracts.Surveillance.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/surveillance")]
[Authorize(Policy = "AdminOnly")]
public class SurveillanceController(ISurveillanceClient surveillance) : ControllerBase
{
    // === Cameras ===

    [HttpPost("cameras")]
    public async Task<IActionResult> RegisterCamera([FromBody] RegisterCameraRequest request, CancellationToken ct)
        => Ok(await surveillance.RegisterCameraAsync(request, ct));

    [HttpGet("cameras")]
    public async Task<IActionResult> ListCameras([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] CameraStatus? status = null, CancellationToken ct = default)
        => Ok(await surveillance.ListCamerasAsync(page, pageSize, status, ct));

    [HttpGet("cameras/{id:guid}")]
    public async Task<IActionResult> GetCamera(Guid id, CancellationToken ct)
        => Ok(await surveillance.GetCameraAsync(id, ct));

    [HttpPut("cameras/{id:guid}/verify")]
    public async Task<IActionResult> VerifyCamera(Guid id, CancellationToken ct)
        => Ok(await surveillance.VerifyCameraAsync(id, ct));

    [HttpDelete("cameras/{id:guid}")]
    public async Task<IActionResult> DeactivateCamera(Guid id, CancellationToken ct)
    {
        await surveillance.DeactivateCameraAsync(id, ct);
        return NoContent();
    }

    // === Footage ===

    [HttpPost("footage")]
    public async Task<IActionResult> SubmitFootage([FromBody] SubmitFootageRequest request, CancellationToken ct)
        => Ok(await surveillance.SubmitFootageAsync(request, ct));

    [HttpGet("footage")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> ListFootage([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] FootageStatus? status = null, CancellationToken ct = default)
        => Ok(await surveillance.ListFootageAsync(page, pageSize, status, ct));

    [HttpGet("footage/{id:guid}")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> GetFootage(Guid id, CancellationToken ct)
        => Ok(await surveillance.GetFootageAsync(id, ct));

    [HttpGet("footage/{id:guid}/detections")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> GetFootageDetections(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await surveillance.GetFootageDetectionsAsync(id, page, pageSize, ct));

    // === Crime Locations ===

    [HttpPost("crime-locations")]
    public async Task<IActionResult> ReportCrimeLocation([FromBody] ReportCrimeLocationRequest request, CancellationToken ct)
        => Ok(await surveillance.ReportCrimeLocationAsync(request, ct));

    [HttpGet("crime-locations")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> ListCrimeLocations([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? activeOnly = true, CancellationToken ct = default)
        => Ok(await surveillance.ListCrimeLocationsAsync(page, pageSize, activeOnly, ct));

    [HttpGet("crime-locations/{id:guid}")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> GetCrimeLocation(Guid id, CancellationToken ct)
        => Ok(await surveillance.GetCrimeLocationAsync(id, ct));

    [HttpGet("crime-locations/{id:guid}/footage")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> GetFootageNearCrimeLocation(Guid id, [FromQuery] double radiusKm = 2.0, CancellationToken ct = default)
        => Ok(await surveillance.GetFootageNearCrimeLocationAsync(id, radiusKm, ct));

    // === Search & Stats ===

    [HttpPost("search")]
    [Authorize(Policy = "ResponderAccess")]
    public async Task<IActionResult> Search([FromBody] SurveillanceSearchRequest request, CancellationToken ct)
        => Ok(await surveillance.SearchAsync(request, ct));

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await surveillance.GetStatsAsync(ct));
}
