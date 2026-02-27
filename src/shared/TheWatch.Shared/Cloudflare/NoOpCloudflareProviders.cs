using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// No-op CDN provider for development/testing. All operations are pass-through.
/// </summary>
public class NoOpCdnProvider : ICdnProvider
{
    private readonly ILogger<NoOpCdnProvider> _logger;
    public NoOpCdnProvider(ILogger<NoOpCdnProvider> logger) => _logger = logger;
    public bool IsConfigured => false;

    public Task<CdnPurgeResult> PurgeCacheAsync(IEnumerable<string> urls, CancellationToken ct)
    {
        _logger.LogDebug("NoOp CDN: PurgeCacheAsync called");
        return Task.FromResult(new CdnPurgeResult(true));
    }

    public Task<CdnPurgeResult> PurgeAllAsync(CancellationToken ct)
    {
        _logger.LogDebug("NoOp CDN: PurgeAllAsync called");
        return Task.FromResult(new CdnPurgeResult(true));
    }

    public Task<CdnPurgeResult> PurgeByTagAsync(IEnumerable<string> tags, CancellationToken ct)
    {
        _logger.LogDebug("NoOp CDN: PurgeByTagAsync called");
        return Task.FromResult(new CdnPurgeResult(true));
    }

    public Task<CdnAnalytics> GetAnalyticsAsync(DateTime since, DateTime until, CancellationToken ct)
    {
        _logger.LogDebug("NoOp CDN: GetAnalyticsAsync called");
        return Task.FromResult(new CdnAnalytics());
    }
}

/// <summary>
/// No-op edge auth provider for development/testing. All tokens are considered valid.
/// </summary>
public class NoOpEdgeAuthProvider : IEdgeAuthProvider
{
    private readonly ILogger<NoOpEdgeAuthProvider> _logger;
    public NoOpEdgeAuthProvider(ILogger<NoOpEdgeAuthProvider> logger) => _logger = logger;
    public bool IsConfigured => false;

    public Task<EdgeAuthResult> ValidateAccessTokenAsync(string token, CancellationToken ct)
    {
        _logger.LogDebug("NoOp EdgeAuth: ValidateAccessTokenAsync — pass-through");
        return Task.FromResult(new EdgeAuthResult(true, Email: "dev@localhost"));
    }

    public Task<EdgeIdentity?> GetIdentityAsync(string accessToken, CancellationToken ct)
    {
        _logger.LogDebug("NoOp EdgeAuth: GetIdentityAsync — returning dev identity");
        return Task.FromResult<EdgeIdentity?>(new EdgeIdentity
        {
            Email = "dev@localhost",
            Name = "Development",
            AuthenticatedAt = DateTime.UtcNow
        });
    }
}

/// <summary>
/// No-op WAF provider for development/testing. All requests allowed.
/// </summary>
public class NoOpWafProvider : IWafProvider
{
    private readonly ILogger<NoOpWafProvider> _logger;
    public NoOpWafProvider(ILogger<NoOpWafProvider> logger) => _logger = logger;
    public bool IsConfigured => false;

    public Task<WafDeployResult> DeployRulesAsync(IEnumerable<WafCustomRule> rules, CancellationToken ct)
    {
        _logger.LogDebug("NoOp WAF: DeployRulesAsync called");
        return Task.FromResult(new WafDeployResult(true));
    }

    public Task<WafDeployResult> DeployRateLimitsAsync(IEnumerable<WafRateLimitRule> rules, CancellationToken ct)
    {
        _logger.LogDebug("NoOp WAF: DeployRateLimitsAsync called");
        return Task.FromResult(new WafDeployResult(true));
    }

    public Task<List<WafEvent>> GetRecentEventsAsync(int limit, CancellationToken ct)
    {
        _logger.LogDebug("NoOp WAF: GetRecentEventsAsync called");
        return Task.FromResult(new List<WafEvent>());
    }

    public Task<bool> BlockIpAsync(string ipAddress, string reason, TimeSpan duration, CancellationToken ct)
    {
        _logger.LogDebug("NoOp WAF: BlockIpAsync called for {Ip}", ipAddress);
        return Task.FromResult(true);
    }
}

/// <summary>
/// No-op tunnel provider for development/testing. Direct local access.
/// </summary>
public class NoOpTunnelProvider : ITunnelProvider
{
    private readonly ILogger<NoOpTunnelProvider> _logger;
    public NoOpTunnelProvider(ILogger<NoOpTunnelProvider> logger) => _logger = logger;
    public bool IsConfigured => false;

    public Task<TunnelStatus> GetTunnelStatusAsync(CancellationToken ct)
    {
        return Task.FromResult(new TunnelStatus(false, "none", Hostname: "localhost"));
    }

    public Task<ZeroTrustValidationResult> ValidateServiceTokenAsync(
        string cfAccessClientId, string cfAccessClientSecret, CancellationToken ct)
    {
        _logger.LogDebug("NoOp Tunnel: ValidateServiceTokenAsync — pass-through");
        return Task.FromResult(new ZeroTrustValidationResult(true, ServiceName: "dev-local"));
    }

    public Task<List<TunnelConnection>> GetConnectionsAsync(CancellationToken ct)
    {
        return Task.FromResult(new List<TunnelConnection>());
    }
}
