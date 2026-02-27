using System.Security.Cryptography;
using System.Text;

namespace TheWatch.Shared.Security;

/// <summary>
/// Password history tracking service for DISA STIG V-222546 compliance.
/// Ensures users cannot reuse any of their last N passwords. The service
/// stores only hashed passwords — never plaintext.
/// </summary>
/// <remarks>
/// STIG requirement: "The application must prohibit password reuse for a minimum
/// of five generations." Default <c>generationsToCheck</c> is 5.
/// </remarks>
public interface IPasswordHistoryService
{
    /// <summary>
    /// Checks whether a password hash matches any of the user's previous passwords
    /// within the specified number of generations.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="passwordHash">The hash of the proposed new password.</param>
    /// <param name="generationsToCheck">
    /// Number of previous password generations to check (default: 5 per DISA STIG V-222546).
    /// </param>
    /// <returns>
    /// <c>true</c> if the password hash was found in history (password reuse detected);
    /// <c>false</c> if the password is not in recent history.
    /// </returns>
    Task<bool> IsPasswordInHistoryAsync(Guid userId, string passwordHash, int generationsToCheck = 5);

    /// <summary>
    /// Records a password change by storing the password hash in the user's history.
    /// Call this after a successful password update.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="passwordHash">The hash of the new password to record.</param>
    Task RecordPasswordChangeAsync(Guid userId, string passwordHash);
}

/// <summary>
/// Entity representing a single entry in a user's password history.
/// Stored in the P5 AuthSecurity database via EF Core.
/// </summary>
public class PasswordHistory
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }

    /// <summary>The user this password history entry belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The hashed password. This is the output of the password hasher
    /// (e.g., PBKDF2 or Argon2id format string), not raw plaintext.
    /// </summary>
    public string HashedPassword { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this password was set.</summary>
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// In-memory password history service for development and testing.
/// Production deployments should use the EF Core implementation in P5.AuthSecurity.
/// </summary>
public class InMemoryPasswordHistoryService : IPasswordHistoryService
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, List<PasswordHistoryEntry>> _history = new();

    private readonly record struct PasswordHistoryEntry(string HashedPassword, DateTime ChangedAtUtc);

    /// <inheritdoc />
    public Task<bool> IsPasswordInHistoryAsync(Guid userId, string passwordHash, int generationsToCheck = 5)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        lock (_lock)
        {
            if (!_history.TryGetValue(userId, out var entries))
                return Task.FromResult(false);

            // Check the most recent N entries
            var recentEntries = entries
                .OrderByDescending(e => e.ChangedAtUtc)
                .Take(generationsToCheck);

            foreach (var entry in recentEntries)
            {
                // Constant-time comparison to prevent timing attacks
                var storedBytes = Encoding.UTF8.GetBytes(entry.HashedPassword);
                var proposedBytes = Encoding.UTF8.GetBytes(passwordHash);

                if (storedBytes.Length == proposedBytes.Length &&
                    CryptographicOperations.FixedTimeEquals(storedBytes, proposedBytes))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task RecordPasswordChangeAsync(Guid userId, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        lock (_lock)
        {
            if (!_history.TryGetValue(userId, out var entries))
            {
                entries = [];
                _history[userId] = entries;
            }

            entries.Add(new PasswordHistoryEntry(passwordHash, DateTime.UtcNow));
        }

        return Task.CompletedTask;
    }
}
