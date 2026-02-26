namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// WAF (Web Application Firewall) management provider interface (Item 138).
///
/// Implementations:
///   - NoOpWafProvider: development/testing (all requests allowed)
///   - CloudflareWafProvider: Cloudflare WAF API (implement in batch)
///
/// Toggle via Cloudflare:UseWaf = true in appsettings.json.
/// </summary>
public interface IWafProvider
{
    /// <summary>
    /// Deploy WAF rules from configuration to Cloudflare.
    /// </summary>
    Task<WafDeployResult> DeployRulesAsync(
        IEnumerable<WafCustomRule> rules, CancellationToken ct = default);

    /// <summary>
    /// Deploy rate limiting rules to Cloudflare.
    /// </summary>
    Task<WafDeployResult> DeployRateLimitsAsync(
        IEnumerable<WafRateLimitRule> rules, CancellationToken ct = default);

    /// <summary>
    /// Get current WAF events/blocks for analysis.
    /// </summary>
    Task<List<WafEvent>> GetRecentEventsAsync(
        int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Temporarily block an IP address.
    /// </summary>
    Task<bool> BlockIpAsync(
        string ipAddress, string reason, TimeSpan duration, CancellationToken ct = default);

    bool IsConfigured { get; }
}

// ─── DTOs ───

public record WafDeployResult(bool Success, int RulesDeployed = 0, string? Error = null);

public record WafEvent
{
    public DateTime Timestamp { get; init; }
    public string ClientIp { get; init; } = string.Empty;
    public string RequestUrl { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public WafAction Action { get; init; }
    public string Country { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
}
