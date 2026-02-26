namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// Cloudflare Tunnel (formerly Argo Tunnel) management provider (Items 139-140).
/// Combines Zero Trust access policies and tunnel management.
///
/// Implementations:
///   - NoOpTunnelProvider: development/testing (local direct access)
///   - CloudflareTunnelProvider: Cloudflare API for tunnels and Zero Trust (implement in batch)
///
/// Toggle via Cloudflare:UseZeroTrust and/or Cloudflare:UseArgoTunnels = true.
/// </summary>
public interface ITunnelProvider
{
    /// <summary>
    /// Get the status of the Cloudflare Tunnel.
    /// </summary>
    Task<TunnelStatus> GetTunnelStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Validate a Cloudflare Access service token for Zero Trust.
    /// </summary>
    Task<ZeroTrustValidationResult> ValidateServiceTokenAsync(
        string cfAccessClientId, string cfAccessClientSecret, CancellationToken ct = default);

    /// <summary>
    /// Get tunnel connection info for diagnostics.
    /// </summary>
    Task<List<TunnelConnection>> GetConnectionsAsync(CancellationToken ct = default);

    bool IsConfigured { get; }
}

// ─── DTOs ───

public record TunnelStatus(
    bool IsConnected,
    string TunnelId,
    string? Hostname = null,
    int ActiveConnections = 0,
    DateTime? ConnectedSince = null);

public record ZeroTrustValidationResult(
    bool IsValid,
    string? ServiceName = null,
    string? Error = null);

public record TunnelConnection(
    string ConnectionId,
    string ColoName,
    bool IsAlive,
    DateTime ConnectedAt,
    string OriginIp);
