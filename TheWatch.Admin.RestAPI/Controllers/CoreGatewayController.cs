using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Contracts.CoreGateway.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/gateway")]
[Authorize(Policy = "AdminOnly")]
public class CoreGatewayController(ICoreGatewayClient gateway) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<IActionResult> ListProfiles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] UserRole? role = null, CancellationToken ct = default)
        => Ok(await gateway.ListProfilesAsync(page, pageSize, role, ct));

    [HttpGet("profiles/{id:guid}")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
        => Ok(await gateway.GetProfileAsync(id, ct));

    [HttpPost("profiles")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request, CancellationToken ct)
        => Ok(await gateway.CreateProfileAsync(request, ct));

    [HttpPut("profiles/{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await gateway.UpdateProfileAsync(id, request, ct));

    [HttpDelete("profiles/{id:guid}")]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken ct)
    {
        await gateway.DeleteProfileAsync(id, ct);
        return NoContent();
    }

    [HttpPost("profiles/{id:guid}/preferences")]
    public async Task<IActionResult> SetPreference(Guid id, [FromBody] SetPreferenceRequest request, CancellationToken ct)
    {
        await gateway.SetPreferenceAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("config/{key}")]
    public async Task<IActionResult> GetConfig(string key, CancellationToken ct)
        => Ok(await gateway.GetConfigAsync(key, ct));

    [HttpPost("config")]
    public async Task<IActionResult> SetConfig([FromBody] SetConfigRequest request, CancellationToken ct)
    {
        await gateway.SetConfigAsync(request, ct);
        return NoContent();
    }

    [HttpGet("services/health")]
    public async Task<IActionResult> GetServiceHealth(CancellationToken ct)
        => Ok(await gateway.GetServiceHealthAsync(ct));
}
