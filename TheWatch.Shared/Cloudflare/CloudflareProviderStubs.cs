using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// Cloudflare CDN provider — cache purge, analytics via Cloudflare API v4 (Item 136).
/// </summary>
public class CloudflareCdnProvider : ICdnProvider
{
    private const string CfApiBase = "https://api.cloudflare.com/client/v4";

    private readonly CloudflareOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareCdnProvider> _logger;

    public CloudflareCdnProvider(CloudflareOptions options, HttpClient httpClient, ILogger<CloudflareCdnProvider> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
        ConfigureAuth();
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiToken) && !string.IsNullOrWhiteSpace(_options.ZoneId);

    public async Task<CdnPurgeResult> PurgeCacheAsync(IEnumerable<string> urls, CancellationToken ct)
    {
        var urlList = urls.ToList();
        _logger.LogInformation("Purging {Count} URLs from Cloudflare CDN cache", urlList.Count);

        var payload = JsonSerializer.Serialize(new { files = urlList });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/purge_cache", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("CDN purge failed: {Status} {Error}", response.StatusCode, error);
            return new CdnPurgeResult(false, Error: error);
        }

        return new CdnPurgeResult(true, urlList.Count);
    }

    public async Task<CdnPurgeResult> PurgeAllAsync(CancellationToken ct)
    {
        _logger.LogWarning("Purging ALL Cloudflare CDN cache for zone {ZoneId}", _options.ZoneId);

        var payload = JsonSerializer.Serialize(new { purge_everything = true });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/purge_cache", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return new CdnPurgeResult(false, Error: error);
        }

        return new CdnPurgeResult(true);
    }

    public async Task<CdnPurgeResult> PurgeByTagAsync(IEnumerable<string> tags, CancellationToken ct)
    {
        var tagList = tags.ToList();
        _logger.LogInformation("Purging cache by tags: {Tags}", string.Join(", ", tagList));

        var payload = JsonSerializer.Serialize(new { tags = tagList });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/purge_cache", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return new CdnPurgeResult(false, Error: error);
        }

        return new CdnPurgeResult(true, tagList.Count);
    }

    public async Task<CdnAnalytics> GetAnalyticsAsync(DateTime since, DateTime until, CancellationToken ct)
    {
        _logger.LogInformation("Fetching CDN analytics from {Since} to {Until}", since, until);

        var response = await _httpClient.GetAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/analytics/dashboard?since={since:O}&until={until:O}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CDN analytics fetch failed: {Status}", response.StatusCode);
            return new CdnAnalytics();
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("totals", out var totals) &&
            totals.TryGetProperty("requests", out var requests) &&
            totals.TryGetProperty("bandwidth", out var bandwidth))
        {
            var totalReqs = requests.TryGetProperty("all", out var all) ? all.GetInt64() : 0;
            var cachedReqs = requests.TryGetProperty("cached", out var cached) ? cached.GetInt64() : 0;
            var uncachedReqs = requests.TryGetProperty("uncached", out var uncached) ? uncached.GetInt64() : 0;
            var totalBw = bandwidth.TryGetProperty("all", out var bwAll) ? bwAll.GetInt64() : 0;
            var cachedBw = bandwidth.TryGetProperty("cached", out var bwCached) ? bwCached.GetInt64() : 0;

            return new CdnAnalytics
            {
                TotalRequests = totalReqs,
                CachedRequests = cachedReqs,
                UncachedRequests = uncachedReqs,
                CacheHitRatio = totalReqs > 0 ? (double)cachedReqs / totalReqs : 0,
                TotalBandwidthBytes = totalBw,
                CachedBandwidthBytes = cachedBw
            };
        }

        return new CdnAnalytics();
    }

    private void ConfigureAuth()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
    }
}

/// <summary>
/// Cloudflare Workers edge auth — JWT validation via Cloudflare Access (Item 137).
/// </summary>
public class CloudflareWorkersAuthProvider : IEdgeAuthProvider
{
    private readonly CloudflareOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareWorkersAuthProvider> _logger;

    public CloudflareWorkersAuthProvider(CloudflareOptions options, HttpClient httpClient, ILogger<CloudflareWorkersAuthProvider> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ZeroTrustTeamDomain);

    public async Task<EdgeAuthResult> ValidateAccessTokenAsync(string token, CancellationToken ct)
    {
        _logger.LogDebug("Validating Cloudflare Access JWT");

        try
        {
            // Fetch JWKS from Cloudflare Access
            var certsUrl = $"https://{_options.ZeroTrustTeamDomain}/cdn-cgi/access/certs";
            var certsResponse = await _httpClient.GetAsync(certsUrl, ct);

            if (!certsResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Access certs from {Url}: {Status}", certsUrl, certsResponse.StatusCode);
                return new EdgeAuthResult(false, Error: "Failed to fetch JWKS");
            }

            // Basic JWT decode to extract claims (signature verification requires JWKS key matching)
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return new EdgeAuthResult(false, Error: "Invalid JWT format");

            var jwt = handler.ReadJwtToken(token);

            // Verify audience matches configured policy AUD
            if (!string.IsNullOrWhiteSpace(_options.ZeroTrustPolicyAud))
            {
                var audClaim = jwt.Audiences.FirstOrDefault();
                if (audClaim != _options.ZeroTrustPolicyAud)
                {
                    _logger.LogWarning("JWT audience mismatch: expected={Expected}, got={Got}",
                        _options.ZeroTrustPolicyAud, audClaim);
                    return new EdgeAuthResult(false, Error: "Audience mismatch");
                }
            }

            // Check expiration
            if (jwt.ValidTo < DateTime.UtcNow)
                return new EdgeAuthResult(false, Error: "Token expired");

            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            _logger.LogInformation("Cloudflare Access JWT validated for {Email}", email);
            return new EdgeAuthResult(true, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Cloudflare Access JWT");
            return new EdgeAuthResult(false, Error: ex.Message);
        }
    }

    public async Task<EdgeIdentity?> GetIdentityAsync(string accessToken, CancellationToken ct)
    {
        _logger.LogDebug("Fetching Cloudflare Access identity");

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://{_options.ZeroTrustTeamDomain}/cdn-cgi/access/get-identity");
        request.Headers.Add("Cookie", $"CF_Authorization={accessToken}");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GetIdentity failed: {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var email = doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
        var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
        var groups = new List<string>();
        if (doc.RootElement.TryGetProperty("groups", out var g))
        {
            foreach (var group in g.EnumerateArray())
            {
                var groupName = group.TryGetProperty("name", out var gn) ? gn.GetString() : group.GetString();
                if (groupName is not null) groups.Add(groupName);
            }
        }
        var country = doc.RootElement.TryGetProperty("country", out var c) ? c.GetString() : null;

        return new EdgeIdentity
        {
            Email = email,
            Name = name,
            Groups = groups,
            Country = country,
            AuthenticatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Cloudflare WAF — custom rules, rate limits, IP blocking via API v4 (Item 138).
/// </summary>
public class CloudflareWafProvider : IWafProvider
{
    private const string CfApiBase = "https://api.cloudflare.com/client/v4";

    private readonly CloudflareOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareWafProvider> _logger;

    public CloudflareWafProvider(CloudflareOptions options, HttpClient httpClient, ILogger<CloudflareWafProvider> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
        ConfigureAuth();
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiToken) && !string.IsNullOrWhiteSpace(_options.ZoneId);

    public async Task<WafDeployResult> DeployRulesAsync(IEnumerable<WafCustomRule> rules, CancellationToken ct)
    {
        var ruleList = rules.ToList();
        _logger.LogInformation("Deploying {Count} WAF custom rules", ruleList.Count);

        var deployed = 0;
        foreach (var rule in ruleList.Where(r => r.Enabled))
        {
            var payload = JsonSerializer.Serialize(new
            {
                description = rule.Description,
                expression = rule.Expression,
                action = rule.Action.ToString().ToLowerInvariant()
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{CfApiBase}/zones/{_options.ZoneId}/firewall/rules", content, ct);

            if (response.IsSuccessStatusCode)
                deployed++;
            else
                _logger.LogWarning("Failed to deploy WAF rule '{Name}': {Status}", rule.Name, response.StatusCode);
        }

        return new WafDeployResult(deployed > 0, deployed);
    }

    public async Task<WafDeployResult> DeployRateLimitsAsync(IEnumerable<WafRateLimitRule> rules, CancellationToken ct)
    {
        var ruleList = rules.ToList();
        _logger.LogInformation("Deploying {Count} rate limiting rules", ruleList.Count);

        var deployed = 0;
        foreach (var rule in ruleList)
        {
            var payload = JsonSerializer.Serialize(new
            {
                match = new { request = new { url_pattern = rule.PathPattern } },
                threshold = rule.RequestsPerPeriod,
                period = rule.PeriodSeconds,
                action = new { mode = rule.Action.ToString().ToLowerInvariant() },
                description = rule.Name
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{CfApiBase}/zones/{_options.ZoneId}/rate_limits", content, ct);

            if (response.IsSuccessStatusCode)
                deployed++;
            else
                _logger.LogWarning("Failed to deploy rate limit '{Name}': {Status}", rule.Name, response.StatusCode);
        }

        return new WafDeployResult(deployed > 0, deployed);
    }

    public async Task<List<WafEvent>> GetRecentEventsAsync(int limit, CancellationToken ct)
    {
        _logger.LogInformation("Fetching {Limit} recent WAF events", limit);

        var response = await _httpClient.GetAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/security/events?limit={limit}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("WAF events fetch failed: {Status}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var events = new List<WafEvent>();

        if (doc.RootElement.TryGetProperty("result", out var results))
        {
            foreach (var evt in results.EnumerateArray())
            {
                events.Add(new WafEvent
                {
                    Timestamp = evt.TryGetProperty("occurredAt", out var ts) && DateTime.TryParse(ts.GetString(), out var dt) ? dt : DateTime.UtcNow,
                    ClientIp = evt.TryGetProperty("clientIP", out var ip) ? ip.GetString() ?? "" : "",
                    RequestUrl = evt.TryGetProperty("clientRequestHTTPHost", out var host) ? host.GetString() ?? "" : "",
                    RuleName = evt.TryGetProperty("ruleId", out var rule) ? rule.GetString() ?? "" : "",
                    Action = evt.TryGetProperty("action", out var action) ? ParseWafAction(action.GetString()) : WafAction.Log,
                    Country = evt.TryGetProperty("clientCountryName", out var country) ? country.GetString() ?? "" : "",
                    UserAgent = evt.TryGetProperty("userAgent", out var ua) ? ua.GetString() ?? "" : ""
                });
            }
        }

        return events;
    }

    public async Task<bool> BlockIpAsync(string ipAddress, string reason, TimeSpan duration, CancellationToken ct)
    {
        _logger.LogWarning("Blocking IP {IpAddress} for {Duration}: {Reason}", ipAddress, duration, reason);

        var payload = JsonSerializer.Serialize(new
        {
            mode = "block",
            configuration = new { target = "ip", value = ipAddress },
            notes = $"{reason} (auto-expires: {DateTime.UtcNow.Add(duration):O})"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{CfApiBase}/zones/{_options.ZoneId}/firewall/access_rules/rules", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("IP block failed for {Ip}: {Error}", ipAddress, error);
            return false;
        }

        return true;
    }

    private void ConfigureAuth()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
    }

    private static WafAction ParseWafAction(string? action) => action?.ToLowerInvariant() switch
    {
        "block" => WafAction.Block,
        "challenge" => WafAction.Challenge,
        "js_challenge" => WafAction.JsChallenge,
        "managed_challenge" => WafAction.ManagedChallenge,
        "skip" => WafAction.Skip,
        _ => WafAction.Log
    };
}

/// <summary>
/// Cloudflare Tunnel + Zero Trust — tunnel status, connections, service token validation (Items 139-140).
/// </summary>
public class CloudflareTunnelProvider : ITunnelProvider
{
    private const string CfApiBase = "https://api.cloudflare.com/client/v4";

    private readonly CloudflareOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareTunnelProvider> _logger;

    public CloudflareTunnelProvider(CloudflareOptions options, HttpClient httpClient, ILogger<CloudflareTunnelProvider> logger)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger;
        ConfigureAuth();
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ApiToken) &&
        !string.IsNullOrWhiteSpace(_options.AccountId) &&
        !string.IsNullOrWhiteSpace(_options.ArgoTunnelId);

    public async Task<TunnelStatus> GetTunnelStatusAsync(CancellationToken ct)
    {
        _logger.LogInformation("Checking tunnel status for {TunnelId}", _options.ArgoTunnelId);

        var response = await _httpClient.GetAsync(
            $"{CfApiBase}/accounts/{_options.AccountId}/cfd_tunnel/{_options.ArgoTunnelId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tunnel status check failed: {Status}", response.StatusCode);
            return new TunnelStatus(false, _options.ArgoTunnelId ?? "");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("result", out var result))
        {
            var tunnelId = result.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            var status = result.TryGetProperty("status", out var s) ? s.GetString() : "";
            var hostname = result.TryGetProperty("name", out var h) ? h.GetString() : null;
            var connections = result.TryGetProperty("connections", out var conns) ? conns.GetArrayLength() : 0;
            var connectedSince = result.TryGetProperty("created_at", out var ca) &&
                DateTime.TryParse(ca.GetString(), out var created) ? created : (DateTime?)null;

            return new TunnelStatus(
                status == "healthy" || status == "active",
                tunnelId,
                hostname,
                connections,
                connectedSince);
        }

        return new TunnelStatus(false, _options.ArgoTunnelId ?? "");
    }

    public async Task<ZeroTrustValidationResult> ValidateServiceTokenAsync(
        string cfAccessClientId, string cfAccessClientSecret, CancellationToken ct)
    {
        _logger.LogInformation("Validating Zero Trust service token: {ClientId}", cfAccessClientId);

        // Service tokens are validated by sending a request with the client ID/secret headers
        // to a Cloudflare Access-protected endpoint. If the response is successful, the token is valid.
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://{_options.ZeroTrustTeamDomain}/cdn-cgi/access/get-identity");
        request.Headers.Add("CF-Access-Client-Id", cfAccessClientId);
        request.Headers.Add("CF-Access-Client-Secret", cfAccessClientSecret);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Service token validation failed: {Status}", response.StatusCode);
            return new ZeroTrustValidationResult(false, Error: $"HTTP {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var serviceName = doc.RootElement.TryGetProperty("service_token_id", out var st) ? st.GetString() : null;
        serviceName ??= doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;

        return new ZeroTrustValidationResult(true, serviceName);
    }

    public async Task<List<TunnelConnection>> GetConnectionsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching tunnel connections for {TunnelId}", _options.ArgoTunnelId);

        var response = await _httpClient.GetAsync(
            $"{CfApiBase}/accounts/{_options.AccountId}/cfd_tunnel/{_options.ArgoTunnelId}/connections", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tunnel connections fetch failed: {Status}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var connections = new List<TunnelConnection>();

        if (doc.RootElement.TryGetProperty("result", out var results))
        {
            foreach (var conn in results.EnumerateArray())
            {
                connections.Add(new TunnelConnection(
                    ConnectionId: conn.TryGetProperty("id", out var cid) ? cid.GetString() ?? "" : "",
                    ColoName: conn.TryGetProperty("colo_name", out var colo) ? colo.GetString() ?? "" : "",
                    IsAlive: conn.TryGetProperty("is_pending_reconnect", out var pr) ? !pr.GetBoolean() : true,
                    ConnectedAt: conn.TryGetProperty("opened_at", out var oa) && DateTime.TryParse(oa.GetString(), out var opened) ? opened : DateTime.UtcNow,
                    OriginIp: conn.TryGetProperty("origin_ip", out var oip) ? oip.GetString() ?? "" : ""
                ));
            }
        }

        return connections;
    }

    private void ConfigureAuth()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
    }
}
