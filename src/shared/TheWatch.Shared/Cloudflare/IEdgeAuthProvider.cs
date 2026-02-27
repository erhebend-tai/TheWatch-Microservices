namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// Edge authentication validation provider interface (Item 137).
/// Validates JWT tokens at the Cloudflare Workers edge before requests reach origin.
///
/// Implementations:
///   - NoOpEdgeAuthProvider: development/testing (pass-through)
///   - CloudflareWorkersAuthProvider: Cloudflare Workers edge auth (implement in batch)
///
/// Toggle via Cloudflare:UseWorkersAuth = true in appsettings.json.
/// </summary>
public interface IEdgeAuthProvider
{
    /// <summary>
    /// Validate a Cloudflare Access JWT (CF-Access-JWT-Assertion header).
    /// </summary>
    Task<EdgeAuthResult> ValidateAccessTokenAsync(
        string token, CancellationToken ct = default);

    /// <summary>
    /// Get the Cloudflare Access identity for a request.
    /// </summary>
    Task<EdgeIdentity?> GetIdentityAsync(
        string accessToken, CancellationToken ct = default);

    bool IsConfigured { get; }
}

// ─── DTOs ───

public record EdgeAuthResult(bool IsValid, string? Email = null, string? Error = null);

public record EdgeIdentity
{
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public List<string> Groups { get; init; } = [];
    public string? Country { get; init; }
    public string? DevicePosture { get; init; }
    public DateTime AuthenticatedAt { get; init; }
}
