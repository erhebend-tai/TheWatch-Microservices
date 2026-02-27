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

    // Password age policy — STIG V-222544, V-222545
    /// <summary>UTC timestamp when the password was last changed. Set on registration and each successful password change.</summary>
    public DateTime? PasswordLastChangedUtc { get; set; }
    /// <summary>UTC timestamp before which password changes are blocked (24-hour minimum age). STIG V-222545.</summary>
    public DateTime? PasswordMinAgeEnforcedUntil { get; set; }
}
