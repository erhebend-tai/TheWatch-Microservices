using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TheWatch.P5.AuthSecurity.Middleware;

/// <summary>
/// Per-IP progressive throttling. 5 failures → 30s, 10 → 5min, 20 → 1hr.
/// Independent from Identity lockout (per-account).
/// Item 225: Uses IDistributedCache (Redis-backed in production) instead of
/// ConcurrentDictionary so throttling state is shared across multiple service instances. [NIST IA-2]
/// </summary>
public class IpThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;

    // Maximum TTL for any blocked record (longest block = 1 hour)
    private static readonly TimeSpan MaxBlockDuration = TimeSpan.FromHours(1);

    public IpThrottlingMiddleware(RequestDelegate next, IDistributedCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "";

        // Only throttle auth endpoints
        if (!path.StartsWith("/api/auth/login") && !path.StartsWith("/api/auth/register"))
        {
            await _next(context);
            return;
        }

        var key = $"ipthrottle:{ip}";
        var record = await GetRecordAsync(key);

        if (record?.BlockedUntil > DateTime.UtcNow)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = ((int)(record.BlockedUntil.Value - DateTime.UtcNow).TotalSeconds).ToString();
            await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Try again later." });
            return;
        }

        await _next(context);

        // Track failures (4xx status)
        if (context.Response.StatusCode is >= 400 and < 500)
        {
            record ??= new IpRecord();
            record.FailureCount++;
            record.LastFailure = DateTime.UtcNow;

            record.BlockedUntil = record.FailureCount switch
            {
                >= 20 => DateTime.UtcNow.AddHours(1),
                >= 10 => DateTime.UtcNow.AddMinutes(5),
                >= 5 => DateTime.UtcNow.AddSeconds(30),
                _ => null
            };

            await SetRecordAsync(key, record, MaxBlockDuration);
        }
        else if (context.Response.StatusCode is >= 200 and < 300)
        {
            // Reset on success
            await _cache.RemoveAsync(key);
        }
    }

    private async Task<IpRecord?> GetRecordAsync(string key)
    {
        var data = await _cache.GetStringAsync(key);
        if (data is null) return null;
        try { return JsonSerializer.Deserialize<IpRecord>(data); }
        catch { return null; }
    }

    private async Task SetRecordAsync(string key, IpRecord record, TimeSpan ttl)
    {
        var data = JsonSerializer.Serialize(record);
        await _cache.SetStringAsync(key, data, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    private sealed class IpRecord
    {
        public int FailureCount { get; set; }
        public DateTime LastFailure { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
