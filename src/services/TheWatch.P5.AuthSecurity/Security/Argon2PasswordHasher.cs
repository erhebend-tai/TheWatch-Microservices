using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Security;

/// <summary>
/// Argon2id password hasher with legacy PBKDF2 migration support.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher<WatchUser>
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryCost = 65536; // 64MB
    private const int TimeCost = 4;
    private const int Parallelism = 4;

    public string HashPassword(WatchUser user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing, // Argon2id
            Version = Argon2Version.Nineteen,
            MemoryCost = MemoryCost,
            TimeCost = TimeCost,
            Lanes = Parallelism,
            Threads = Parallelism,
            Salt = salt,
            Password = System.Text.Encoding.UTF8.GetBytes(password),
            HashLength = HashSize
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        return config.EncodeString(hash.Buffer);
    }

    public PasswordVerificationResult VerifyHashedPassword(WatchUser user, string hashedPassword, string providedPassword)
    {
        // Check if this is a legacy PBKDF2 hash (48-byte Base64 = salt(16) + hash(32))
        if (IsLegacyPbkdf2Hash(hashedPassword))
        {
            var legacyValid = VerifyLegacyPbkdf2(providedPassword, hashedPassword);
            return legacyValid ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
        }

        // Argon2id verification
        if (Argon2.Verify(hashedPassword, providedPassword))
            return PasswordVerificationResult.Success;

        return PasswordVerificationResult.Failed;
    }

    private static bool IsLegacyPbkdf2Hash(string hash)
    {
        try
        {
            var bytes = Convert.FromBase64String(hash);
            return bytes.Length == 48; // 16 salt + 32 hash
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyLegacyPbkdf2(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var salt = combined[..16];
        var storedHashBytes = combined[16..];
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
    }
}
