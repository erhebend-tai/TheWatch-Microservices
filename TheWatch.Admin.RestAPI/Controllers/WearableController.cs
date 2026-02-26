using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.Wearable;
using TheWatch.Contracts.Wearable.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/wearable")]
[Authorize(Policy = "AdminOnly")]
public class WearableController(IWearableClient wearable) : ControllerBase
{
    [HttpGet("devices")]
    public async Task<IActionResult> ListDevices([FromQuery] Guid? ownerId = null, CancellationToken ct = default)
        => Ok(await wearable.ListDevicesAsync(ownerId, ct));

    [HttpGet("devices/{id:guid}")]
    public async Task<IActionResult> GetDevice(Guid id, CancellationToken ct)
        => Ok(await wearable.GetDeviceAsync(id, ct));

    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken ct)
        => Ok(await wearable.RegisterDeviceAsync(request, ct));

    [HttpPut("devices/{id:guid}/status")]
    public async Task<IActionResult> UpdateDeviceStatus(Guid id, [FromBody] UpdateDeviceStatusRequest request, CancellationToken ct)
        => Ok(await wearable.UpdateDeviceStatusAsync(id, request, ct));

    [HttpDelete("devices/{id:guid}")]
    public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken ct)
    {
        await wearable.DeleteDeviceAsync(id, ct);
        return NoContent();
    }

    [HttpPost("devices/{deviceId:guid}/heartbeat")]
    public async Task<IActionResult> RecordHeartbeat(Guid deviceId, [FromBody] RecordHeartbeatRequest request, CancellationToken ct)
        => Ok(await wearable.RecordHeartbeatAsync(deviceId, request, ct));

    [HttpGet("devices/{deviceId:guid}/heartbeat")]
    public async Task<IActionResult> GetHeartbeatHistory(Guid deviceId, CancellationToken ct)
        => Ok(await wearable.GetHeartbeatHistoryAsync(deviceId, ct));

    [HttpPost("devices/{deviceId:guid}/sync")]
    public async Task<IActionResult> StartSync(Guid deviceId, [FromBody] StartSyncRequest request, CancellationToken ct)
        => Ok(await wearable.StartSyncAsync(deviceId, request, ct));
}
