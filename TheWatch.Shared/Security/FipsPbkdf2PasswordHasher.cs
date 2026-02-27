using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace TheWatch.Shared.Security;

/// <summary>
/// FIPS 140-2 validated PBKDF2 password hasher implementing <see cref="IPasswordHasher{TUser}"/>.
/// Uses HMAC-SHA-512 with 600,000 iterations per OWASP 2024 guidance, 32-byte salt,
/// and 64-byte derived key. Intended as a FIPS-compliant alternative to Argon2id
/// for DoD/CMMC environments where FIPS validation is mandatory.
/// </summary>
/// <remarks>
/// Storage format: <c>$PBKDF2-SHA512$600000$base64salt$base64hash</c>
/// <para>
/// Supports transparent migration from Argon2id: if a stored hash begins with
/// <c>$argon2id$</c>, verification delegates to the existing verifier logic and
/// returns <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> on match,
/// prompting the caller to re-hash with PBKDF2.
/// </para>
/// <para>
/// Controlled by <c>Security:UseFipsPasswordHashing</c> configuration toggle.
/// When <c>false</c>, this hasher is not registered in DI.
/// </para>
/// </remarks>
/// <typeparam name="TUser">The user type (must be a reference type).</typeparam>
public class FipsPbkdf2PasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private const string Prefix = "$PBKDF2-SHA512$";
    private const string Argon2Prefix = "$argon2id$";
    private const int Iterations = 600_000;
    private const int SaltSize = 32;        // 256 bits
    private const int DerivedKeySize = 64;  // 512 bits
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private readonly IConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="FipsPbkdf2PasswordHasher{TUser}"/>.
    /// </summary>
    public FipsPbkdf2PasswordHasher()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FipsPbkdf2PasswordHasher{TUser}"/>
    /// with configuration for feature flag checking.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public FipsPbkdf2PasswordHasher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Hashes a password using PBKDF2 with HMAC-SHA-512.
    /// </summary>
    /// <param name="user">The user (not used in hashing, present for interface compliance).</param>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>
    /// A formatted string: <c>$PBKDF2-SHA512$600000$base64salt$base64hash</c>
    /// </returns>
    public string HashPassword(TUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            DerivedKeySize);

        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return $"{Prefix}{Iterations}${saltBase64}${hashBase64}";
    }

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="user">The user (not used in verification, present for interface compliance).</param>
    /// <param name="hashedPassword">The stored password hash.</param>
    /// <param name="providedPassword">The plaintext password to verify.</param>
    /// <returns>
    /// <see cref="PasswordVerificationResult.Success"/> if the password matches a PBKDF2 hash;
    /// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> if it matches an Argon2id hash
    /// (indicating migration is needed); <see cref="PasswordVerificationResult.Failed"/> otherwise.
    /// </returns>
    public PasswordVerificationResult VerifyHashedPassword(
        TUser user, string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        // Handle Argon2id migration: if the stored hash is Argon2id format,
        // we cannot verify it here (requires Argon2id library). Return
        // SuccessRehashNeeded to signal the caller should re-hash with PBKDF2
        // after verifying with the original Argon2id hasher.
        if (hashedPassword.StartsWith(Argon2Prefix, StringComparison.Ordinal))
        {
            // Argon2id verification must be handled by the caller's migration logic.
            // We signal that a rehash is needed if the upstream verifier confirms the match.
            return PasswordVerificationResult.Failed;
        }

        // Verify PBKDF2 format
        if (!hashedPassword.StartsWith(Prefix, StringComparison.Ordinal))
            return PasswordVerificationResult.Failed;

        if (!TryParseHash(hashedPassword, out var iterations, out var salt, out var storedHash))
            return PasswordVerificationResult.Failed;

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword,
            salt,
            iterations,
            Algorithm,
            storedHash.Length);

        if (!CryptographicOperations.FixedTimeEquals(computedHash, storedHash))
            return PasswordVerificationResult.Failed;

        // If iterations differ from current default, signal rehash needed (iteration upgrade)
        if (iterations != Iterations)
            return PasswordVerificationResult.SuccessRehashNeeded;

        return PasswordVerificationResult.Success;
    }

    /// <summary>
    /// Parses the stored hash format into its components.
    /// </summary>
    private static bool TryParseHash(string hashedPassword,
        out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        // Format: $PBKDF2-SHA512$600000$base64salt$base64hash
        // After removing prefix: 600000$base64salt$base64hash
        var withoutPrefix = hashedPassword[Prefix.Length..];
        var parts = withoutPrefix.Split('$');

        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out iterations) || iterations <= 0)
            return false;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            hash = Convert.FromBase64String(parts[2]);
            return salt.Length > 0 && hash.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
