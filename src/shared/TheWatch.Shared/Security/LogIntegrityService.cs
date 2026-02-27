using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TheWatch.Shared.Security;

/// <summary>
/// Provides HMAC-SHA256 log entry signing with blockchain-style chaining for
/// tamper detection of audit logs. Each log entry's HMAC incorporates the previous
/// entry's HMAC, forming an unbroken chain. If any entry is modified, inserted,
/// or removed, the chain verification fails.
/// </summary>
/// <remarks>
/// Implements NIST SP 800-92 audit log integrity requirements and CMMC Level 2
/// control AU.L2-3.3.1 (system audit log protection).
/// </remarks>
public interface ILogIntegrityService
{
    /// <summary>
    /// Computes an HMAC-SHA256 for a log entry, chaining it to the previous entry's HMAC.
    /// </summary>
    /// <param name="logEntry">The log entry text to sign.</param>
    /// <param name="previousHmac">The HMAC of the previous log entry, or empty string for the first entry.</param>
    /// <returns>The HMAC as a lowercase hexadecimal string.</returns>
    string ComputeHmac(string logEntry, string previousHmac);

    /// <summary>
    /// Verifies an entire chain of log entries by recomputing each HMAC and comparing
    /// against the stored value.
    /// </summary>
    /// <param name="chain">Ordered sequence of (entry text, stored HMAC) tuples.</param>
    /// <returns><c>true</c> if the chain is intact; <c>false</c> if any entry has been tampered with.</returns>
    bool VerifyChain(IEnumerable<(string Entry, string Hmac)> chain);
}

/// <summary>
/// Thread-safe HMAC-SHA256 log integrity service. The signing key is loaded from
/// <c>Security:LogSigningKey</c> configuration. The key must be at least 32 bytes
/// (256 bits) when UTF-8 encoded.
/// </summary>
public class LogIntegrityService : ILogIntegrityService
{
    private readonly byte[] _signingKey;
    private readonly object _lock = new();
    private string _lastHmac = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="LogIntegrityService"/>.
    /// </summary>
    /// <param name="configuration">Application configuration containing <c>Security:LogSigningKey</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>Security:LogSigningKey</c> is not configured or is too short.
    /// </exception>
    public LogIntegrityService(IConfiguration configuration)
    {
        var keyString = configuration["Security:LogSigningKey"]
            ?? throw new InvalidOperationException(
                "FATAL: Security:LogSigningKey is not configured. " +
                "A minimum 32-byte key is required for HMAC-SHA256 log signing.");

        _signingKey = Encoding.UTF8.GetBytes(keyString);

        if (_signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Security:LogSigningKey must be at least 32 bytes (256 bits) when UTF-8 encoded.");
        }
    }

    /// <inheritdoc />
    public string ComputeHmac(string logEntry, string previousHmac)
    {
        ArgumentNullException.ThrowIfNull(logEntry);
        previousHmac ??= string.Empty;

        // Chain: HMAC(key, previousHmac + logEntry)
        var payload = previousHmac + logEntry;
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        string hmacHex;
        lock (_lock)
        {
            var hmacBytes = HMACSHA256.HashData(_signingKey, payloadBytes);
            hmacHex = Convert.ToHexStringLower(hmacBytes);
            _lastHmac = hmacHex;
        }

        return hmacHex;
    }

    /// <summary>
    /// Computes an HMAC chained to the internally tracked previous HMAC.
    /// Thread-safe — each call atomically reads the previous HMAC and updates it.
    /// </summary>
    /// <param name="logEntry">The log entry text to sign.</param>
    /// <returns>The HMAC as a lowercase hexadecimal string.</returns>
    public string ComputeChainedHmac(string logEntry)
    {
        lock (_lock)
        {
            return ComputeHmac(logEntry, _lastHmac);
        }
    }

    /// <inheritdoc />
    public bool VerifyChain(IEnumerable<(string Entry, string Hmac)> chain)
    {
        ArgumentNullException.ThrowIfNull(chain);

        var previousHmac = string.Empty;

        foreach (var (entry, storedHmac) in chain)
        {
            var payload = previousHmac + entry;
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var computedBytes = HMACSHA256.HashData(_signingKey, payloadBytes);
            var computedHex = Convert.ToHexStringLower(computedBytes);

            // Constant-time comparison to prevent timing side-channel
            var storedBytes = Encoding.UTF8.GetBytes(storedHmac);
            var computedCompareBytes = Encoding.UTF8.GetBytes(computedHex);

            if (!CryptographicOperations.FixedTimeEquals(storedBytes, computedCompareBytes))
                return false;

            previousHmac = storedHmac;
        }

        return true;
    }
}
