using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.AuthSecurity;
using TheWatch.Contracts.AuthSecurity.Models;

namespace TheWatch.Admin.RestAPI.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AuthSecurityController(IAuthSecurityClient auth) : ControllerBase
{
    /// <summary>
    /// Security+ 1.4: Verify caller owns the target userId or is Admin (prevent IDOR).
    /// </summary>
    private bool CallerCanAccessUser(Guid userId)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (callerId != null && Guid.TryParse(callerId, out var callerGuid) && callerGuid == userId)
            return true;
        return User.IsInRole("Admin");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await auth.LoginAsync(request, ct));

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => Ok(await auth.RegisterAsync(request, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        => Ok(await auth.RefreshTokenAsync(request, ct));

    [HttpGet("users/{userId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserInfo(Guid userId, CancellationToken ct)
        => Ok(await auth.GetUserInfoAsync(userId, ct));

    [HttpPost("roles/assign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        await auth.AssignRoleAsync(request, ct);
        return NoContent();
    }

    [HttpPost("mfa/setup/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> SetupMfa(Guid userId, CancellationToken ct)
    {
        // Security+ 1.4: Prevent IDOR — caller must own the userId or be Admin
        if (!CallerCanAccessUser(userId))
            return Forbid();
        return Ok(await auth.SetupMfaAsync(userId, ct));
    }

    [HttpPost("mfa/verify/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> VerifyMfa(Guid userId, [FromBody] MfaVerifyRequest request, CancellationToken ct)
    {
        // Security+ 1.4: Prevent IDOR — caller must own the userId or be Admin
        if (!CallerCanAccessUser(userId))
            return Forbid();
        await auth.VerifyMfaAsync(userId, request, ct);
        return NoContent();
    }

    [HttpPost("mfa/sms/send")]
    [AllowAnonymous]
    public async Task<IActionResult> SendSmsMfa([FromBody] SmsMfaSendRequest request, CancellationToken ct)
    {
        await auth.SendSmsMfaAsync(request, ct);
        return NoContent();
    }

    [HttpPost("mfa/sms/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifySmsMfa([FromBody] SmsMfaVerifyRequest request, CancellationToken ct)
    {
        await auth.VerifySmsMfaAsync(request, ct);
        return NoContent();
    }

    [HttpPost("magic-link")]
    [AllowAnonymous]
    public async Task<IActionResult> SendMagicLink([FromBody] MagicLinkRequest request, CancellationToken ct)
    {
        await auth.SendMagicLinkAsync(request, ct);
        return NoContent();
    }
}
