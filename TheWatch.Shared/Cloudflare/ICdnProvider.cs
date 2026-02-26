namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// CDN provider interface for static asset caching and purging (Item 136).
///
/// Implementations:
///   - NoOpCdnProvider: development/testing (pass-through)
///   - CloudflareCdnProvider: Cloudflare CDN API (implement in batch)
///
/// Toggle via Cloudflare:UseCdn = true in appsettings.json.
/// </summary>
public interface ICdnProvider
{
    /// <summary>
    /// Purge specific URLs from the CDN cache.
    /// </summary>
    Task<CdnPurgeResult> PurgeCacheAsync(
        IEnumerable<string> urls, CancellationToken ct = default);

    /// <summary>
    /// Purge all cached content for the zone.
    /// </summary>
    Task<CdnPurgeResult> PurgeAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Purge cached content by cache tag.
    /// </summary>
    Task<CdnPurgeResult> PurgeByTagAsync(
        IEnumerable<string> tags, CancellationToken ct = default);

    /// <summary>
    /// Get cache analytics for the zone.
    /// </summary>
    Task<CdnAnalytics> GetAnalyticsAsync(
        DateTime since, DateTime until, CancellationToken ct = default);

    bool IsConfigured { get; }
}

// ─── DTOs ───

public record CdnPurgeResult(bool Success, int PurgedCount = 0, string? Error = null);

public record CdnAnalytics
{
    public long TotalRequests { get; init; }
    public long CachedRequests { get; init; }
    public long UncachedRequests { get; init; }
    public double CacheHitRatio { get; init; }
    public long TotalBandwidthBytes { get; init; }
    public long CachedBandwidthBytes { get; init; }
}
