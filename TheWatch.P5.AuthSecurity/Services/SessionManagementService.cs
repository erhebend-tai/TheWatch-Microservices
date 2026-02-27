using System.Collections.Concurrent;

namespace TheWatch.P5.AuthSecurity.Services;

/// <summary>
/// Tracks active JWT sessions per user and enforces concurrent session limits.
/// DISA STIG V-222581: concurrent session control.
/// Uses ConcurrentDictionary for dev; migrate to Redis for production (distributed session store).
/// </summary>
public sealed class SessionManagementService
{
    // userId -> (jti -> session info)
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, SessionEntry>> _sessions = new();

    /// <summary>
    /// Records a new JWT session for the user. Enforces the maximum session limit
    /// by revoking the oldest sessions if the limit would be exceeded.
    /// </summary>
    public Task RecordSessionAsync(Guid userId, string jti, DateTime expiresAt)
    {
        var userSessions = _sessions.GetOrAdd(userId, _ => new ConcurrentDictionary<string, SessionEntry>());

        // Purge expired sessions first
        PurgeExpired(userSessions);

        userSessions[jti] = new SessionEntry(jti, DateTime.UtcNow, expiresAt);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a specific session (by JTI) is still valid (not revoked, not expired).
    /// </summary>
    public Task<bool> IsSessionValidAsync(Guid userId, string jti)
    {
        if (!_sessions.TryGetValue(userId, out var userSessions))
            return Task.FromResult(false);

        if (!userSessions.TryGetValue(jti, out var entry))
            return Task.FromResult(false);

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            // Expired — clean it up
            userSessions.TryRemove(jti, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Revokes a specific session for the user.
    /// </summary>
    public Task RevokeSessionAsync(Guid userId, string jti)
    {
        if (_sessions.TryGetValue(userId, out var userSessions))
        {
            userSessions.TryRemove(jti, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns all active (non-expired) sessions for the user.
    /// </summary>
    public Task<List<SessionInfo>> GetActiveSessionsAsync(Guid userId)
    {
        if (!_sessions.TryGetValue(userId, out var userSessions))
            return Task.FromResult(new List<SessionInfo>());

        PurgeExpired(userSessions);

        var result = userSessions.Values
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SessionInfo(s.Jti, s.CreatedAt, s.ExpiresAt))
            .ToList();

        return Task.FromResult(result);
    }

    /// <summary>
    /// Enforces the maximum number of concurrent sessions for a user.
    /// If the user has more sessions than the limit, the oldest sessions are revoked.
    /// Returns the list of revoked JTIs (if any).
    /// </summary>
    public Task<List<string>> EnforceSessionLimitAsync(Guid userId, int maxSessions = 5)
    {
        var revoked = new List<string>();

        if (!_sessions.TryGetValue(userId, out var userSessions))
            return Task.FromResult(revoked);

        PurgeExpired(userSessions);

        var activeSessions = userSessions.Values
            .OrderBy(s => s.CreatedAt)
            .ToList();

        if (activeSessions.Count <= maxSessions)
            return Task.FromResult(revoked);

        // Revoke oldest sessions until we are at the limit
        var toRevoke = activeSessions.Count - maxSessions;
        foreach (var session in activeSessions.Take(toRevoke))
        {
            if (userSessions.TryRemove(session.Jti, out _))
            {
                revoked.Add(session.Jti);
            }
        }

        return Task.FromResult(revoked);
    }

    /// <summary>
    /// Removes all expired session entries from the user's session dictionary.
    /// </summary>
    private static void PurgeExpired(ConcurrentDictionary<string, SessionEntry> userSessions)
    {
        var now = DateTime.UtcNow;
        var expired = userSessions.Where(kvp => kvp.Value.ExpiresAt <= now).Select(kvp => kvp.Key).ToList();
        foreach (var jti in expired)
        {
            userSessions.TryRemove(jti, out _);
        }
    }

    /// <summary>Internal session tracking entry.</summary>
    private sealed record SessionEntry(string Jti, DateTime CreatedAt, DateTime ExpiresAt);

    /// <summary>Public session info returned by GetActiveSessionsAsync.</summary>
    public sealed record SessionInfo(string Jti, DateTime CreatedAt, DateTime ExpiresAt);
}
