using System.Collections.Concurrent;

namespace TheWatch.P5.AuthSecurity.Middleware;

/// <summary>
/// Per-IP progressive throttling. 5 failures → 30s, 10 → 5min, 20 → 1hr.
/// Independent from Identity lockout (per-account).
/// </summary>
public class IpThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, IpRecord> _records = new();

    public IpThrottlingMiddleware(RequestDelegate next)
    {
        _next = next;
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

        if (_records.TryGetValue(ip, out var record))
        {
            if (record.BlockedUntil > DateTime.UtcNow)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = ((int)(record.BlockedUntil - DateTime.UtcNow).Value.TotalSeconds).ToString();
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Try again later." });
                return;
            }
        }

        await _next(context);

        // Track failures (4xx status)
        if (context.Response.StatusCode is >= 400 and < 500)
        {
            var rec = _records.GetOrAdd(ip, _ => new IpRecord());
            rec.FailureCount++;
            rec.LastFailure = DateTime.UtcNow;

            rec.BlockedUntil = rec.FailureCount switch
            {
                >= 20 => DateTime.UtcNow.AddHours(1),
                >= 10 => DateTime.UtcNow.AddMinutes(5),
                >= 5 => DateTime.UtcNow.AddSeconds(30),
                _ => null
            };
        }
        else if (context.Response.StatusCode is >= 200 and < 300)
        {
            // Reset on success
            _records.TryRemove(ip, out _);
        }
    }

    private class IpRecord
    {
        public int FailureCount { get; set; }
        public DateTime LastFailure { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
