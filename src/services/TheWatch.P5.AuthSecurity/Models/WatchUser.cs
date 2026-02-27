using Microsoft.AspNetCore.Identity;

namespace TheWatch.P5.AuthSecurity.Models;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// </summary>
public class WatchUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // MFA / TOTP fields
    public string? TotpSecretKey { get; set; }
    public bool IsTotpEnabled { get; set; }
    public string? RecoveryCodes { get; set; }

    // EULA
    public string? AcceptedEulaVersion { get; set; }
    public DateTime? EulaAcceptedAt { get; set; }

    // Onboarding
    public string? OnboardingProgress { get; set; }
}
