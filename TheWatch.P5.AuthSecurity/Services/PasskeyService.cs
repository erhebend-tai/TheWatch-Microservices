using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// WebAuthn/FIDO2 passkey service. Provides registration and authentication flows.
/// Uses a simplified implementation — production should use Fido2.AspNet library.
/// </summary>
public class PasskeyService
{
    private readonly AuthIdentityDbContext _db;
    private readonly UserManager<WatchUser> _userManager;
    private readonly ILogger<PasskeyService> _logger;

    public PasskeyService(AuthIdentityDbContext db, UserManager<WatchUser> userManager, ILogger<PasskeyService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<object> BeginRegistrationAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) throw new InvalidOperationException("User not found.");

        var existingCredentials = await _db.FidoCredentials
            .Where(c => c.UserId == userId)
            .Select(c => new { c.CredentialId })
            .ToListAsync();

        // Return WebAuthn options for client
        return new
        {
            challenge = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            rp = new { id = "thewatch.dev", name = "TheWatch" },
            user = new
            {
                id = Convert.ToBase64String(userId.ToByteArray()),
                name = user.Email,
                displayName = user.DisplayName
            },
            pubKeyCredParams = new[]
            {
                new { type = "public-key", alg = -7 },  // ES256
                new { type = "public-key", alg = -257 }  // RS256
            },
            excludeCredentials = existingCredentials.Select(c => new
            {
                type = "public-key",
                id = Convert.ToBase64String(c.CredentialId)
            }),
            authenticatorSelection = new
            {
                authenticatorAttachment = "platform",
                residentKey = "preferred",
                userVerification = "preferred"
            },
            timeout = 60000
        };
    }

    public async Task<bool> CompleteRegistrationAsync(Guid userId, JsonElement attestation)
    {
        try
        {
            // In production, use Fido2.AspNet for full attestation verification
            var credentialId = attestation.TryGetProperty("credentialId", out var cid)
                ? Convert.FromBase64String(cid.GetString()!)
                : [];
            var publicKey = attestation.TryGetProperty("publicKey", out var pk)
                ? Convert.FromBase64String(pk.GetString()!)
                : [];

            if (credentialId.Length == 0) return false;

            var credential = new FidoCredential
            {
                UserId = userId,
                CredentialId = credentialId,
                PublicKey = publicKey,
                DeviceName = attestation.TryGetProperty("deviceName", out var dn) ? dn.GetString() : "Unknown"
            };

            _db.FidoCredentials.Add(credential);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FIDO2 registration failed for user {UserId}", userId);
            return false;
        }
    }

    public object BeginAuthentication()
    {
        return new
        {
            challenge = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            rpId = "thewatch.dev",
            userVerification = "preferred",
            timeout = 60000
        };
    }

    public async Task<Guid?> CompleteAuthenticationAsync(JsonElement assertion)
    {
        try
        {
            if (!assertion.TryGetProperty("credentialId", out var cid))
                return null;

            var credentialIdBytes = Convert.FromBase64String(cid.GetString()!);
            var credential = await _db.FidoCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialIdBytes);

            if (credential is null) return null;

            // In production: verify signature with stored public key
            credential.SignatureCounter++;
            credential.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return credential.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FIDO2 authentication failed");
            return null;
        }
    }
}
