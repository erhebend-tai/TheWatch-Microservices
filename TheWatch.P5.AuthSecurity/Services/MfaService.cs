using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using TheWatch.P5.AuthSecurity.Auth;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// TOTP MFA service using Identity's built-in authenticator support.
/// </summary>
public class MfaService
{
    private readonly UserManager<WatchUser> _userManager;

    public MfaService(UserManager<WatchUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<MfaSetupResponse> EnableTotpAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Generate authenticator key
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        // Generate recovery codes
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        // Generate authenticator URI for QR code
        var email = await _userManager.GetEmailAsync(user);
        var uri = GenerateQrCodeUri("TheWatch", email!, key!);

        return new MfaSetupResponse(key!, uri, codes!.ToArray());
    }

    public async Task<bool> VerifyTotpAsync(Guid userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (isValid && !user.IsTotpEnabled)
        {
            user.IsTotpEnabled = true;
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _userManager.UpdateAsync(user);
        }

        return isValid;
    }

    public async Task DisableTotpAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        user.IsTotpEnabled = false;
        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateAsync(user);
    }

    private static string GenerateQrCodeUri(string issuer, string email, string key)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }
}
