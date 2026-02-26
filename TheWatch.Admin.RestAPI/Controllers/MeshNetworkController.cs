using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.MeshNetwork;
using TheWatch.Contracts.MeshNetwork.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/mesh")]
[Authorize(Policy = "AdminOnly")]
public class MeshNetworkController(IMeshNetworkClient mesh) : ControllerBase
{
    [HttpGet("nodes")]
    public async Task<IActionResult> ListNodes(CancellationToken ct)
        => Ok(await mesh.ListNodesAsync(ct));

    [HttpGet("nodes/{id:guid}")]
    public async Task<IActionResult> GetNode(Guid id, CancellationToken ct)
        => Ok(await mesh.GetNodeAsync(id, ct));

    [HttpPost("nodes")]
    public async Task<IActionResult> RegisterNode([FromBody] RegisterNodeRequest request, CancellationToken ct)
        => Ok(await mesh.RegisterNodeAsync(request, ct));

    [HttpPut("nodes/{id:guid}/status")]
    public async Task<IActionResult> UpdateNodeStatus(Guid id, [FromBody] UpdateNodeStatusRequest request, CancellationToken ct)
        => Ok(await mesh.UpdateNodeStatusAsync(id, request, ct));

    [HttpDelete("nodes/{id:guid}")]
    public async Task<IActionResult> DeleteNode(Guid id, CancellationToken ct)
    {
        await mesh.DeleteNodeAsync(id, ct);
        return NoContent();
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
        => Ok(await mesh.SendMessageAsync(request, ct));

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] Guid? channelId = null, CancellationToken ct = default)
        => Ok(await mesh.GetMessagesAsync(channelId, ct));

    [HttpPost("channels")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request, CancellationToken ct)
        => Ok(await mesh.CreateChannelAsync(request, ct));

    [HttpGet("channels")]
    public async Task<IActionResult> ListChannels(CancellationToken ct)
        => Ok(await mesh.ListChannelsAsync(ct));

    [HttpGet("topology")]
    public async Task<IActionResult> GetTopology(CancellationToken ct)
        => Ok(await mesh.GetTopologyAsync(ct));
}
