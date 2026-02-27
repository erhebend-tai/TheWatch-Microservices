using Microsoft.AspNetCore.Identity;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Magic link email authentication service.
/// </summary>
public class MagicLinkService
{
    private readonly UserManager<WatchUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<MagicLinkService> _logger;

    public MagicLinkService(UserManager<WatchUser> userManager, IConfiguration config, ILogger<MagicLinkService> logger)
    {
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    public async Task SendMagicLinkAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Don't reveal user existence — silently succeed
            _logger.LogWarning("Magic link requested for non-existent email: {Email}", email);
            return;
        }

        var token = await _userManager.GenerateUserTokenAsync(user, "Default", "MagicLink");
        var baseUrl = _config["MagicLink:BaseUrl"] ?? "https://localhost:5000";
        var link = $"{baseUrl}/api/auth/magic-link/verify?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        // In production: send via email service
        _logger.LogInformation("Magic link for {Email}: {Link}", email, link);
    }

    public async Task<bool> VerifyMagicLinkAsync(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return false;

        return await _userManager.VerifyUserTokenAsync(user, "Default", "MagicLink", token);
    }
}
