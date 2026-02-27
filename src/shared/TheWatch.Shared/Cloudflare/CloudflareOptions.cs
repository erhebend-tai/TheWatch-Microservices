namespace TheWatch.Shared.Cloudflare;

/// <summary>
/// Central configuration for Cloudflare edge service toggles.
/// Bind from "Cloudflare" section in appsettings.json.
///
/// Each toggle enables a Cloudflare edge capability:
///   - UseCdn: Cloudflare CDN for static assets (MAUI WebView, Dashboard)
///   - UseWorkersAuth: Cloudflare Workers for edge authentication validation
///   - UseWaf: Cloudflare WAF rules for API protection
///   - UseZeroTrust: Cloudflare Zero Trust for admin/dashboard access
///   - UseArgoTunnels: Argo Tunnels for secure service exposure
///
/// When toggles are false, middleware passes through (no-op).
/// When true + credentials configured, Cloudflare integration activates.
/// </summary>
public class CloudflareOptions
{
    public const string SectionName = "Cloudflare";

    /// <summary>
    /// Cloudflare API token with appropriate permissions.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Cloudflare account ID.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Cloudflare zone ID for the domain.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Primary domain (e.g., "thewatch.app").
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    // ─── Item 136: CDN ───

    public bool UseCdn { get; set; }

    /// <summary>
    /// CDN origin URL for static assets.
    /// </summary>
    public string? CdnOriginUrl { get; set; }

    /// <summary>
    /// CDN cache TTL in seconds for static assets.
    /// </summary>
    public int CdnCacheTtlSeconds { get; set; } = 86400;

    /// <summary>
    /// CDN paths to cache (glob patterns, e.g., "/css/*", "/js/*", "/images/*").
    /// </summary>
    public List<string> CdnCachePaths { get; set; } = ["/css/*", "/js/*", "/images/*", "/_content/*"];

    // ─── Item 137: Workers Edge Auth ───

    public bool UseWorkersAuth { get; set; }

    /// <summary>
    /// JWT issuer expected in tokens validated at the edge.
    /// </summary>
    public string? WorkersJwtIssuer { get; set; }

    /// <summary>
    /// JWT audience expected in tokens validated at the edge.
    /// </summary>
    public string? WorkersJwtAudience { get; set; }

    /// <summary>
    /// JWKS endpoint for edge token validation.
    /// </summary>
    public string? WorkersJwksEndpoint { get; set; }

    /// <summary>
    /// Paths that require edge auth validation (e.g., "/api/*").
    /// </summary>
    public List<string> WorkersProtectedPaths { get; set; } = ["/api/*"];

    /// <summary>
    /// Paths excluded from edge auth (e.g., "/health", "/api/auth/login").
    /// </summary>
    public List<string> WorkersExcludedPaths { get; set; } = ["/health", "/alive", "/metrics", "/api/auth/login", "/api/auth/register"];

    // ─── Item 138: WAF ───

    public bool UseWaf { get; set; }

    /// <summary>
    /// WAF managed ruleset IDs to enable.
    /// </summary>
    public List<string> WafManagedRulesets { get; set; } = [];

    /// <summary>
    /// Custom WAF rules.
    /// </summary>
    public List<WafCustomRule> WafCustomRules { get; set; } = [];

    /// <summary>
    /// Rate limiting rules for API protection.
    /// </summary>
    public List<WafRateLimitRule> WafRateLimits { get; set; } = [];

    // ─── Item 139: Zero Trust ───

    public bool UseZeroTrust { get; set; }

    /// <summary>
    /// Zero Trust team domain (e.g., "thewatch.cloudflareaccess.com").
    /// </summary>
    public string? ZeroTrustTeamDomain { get; set; }

    /// <summary>
    /// Paths gated by Zero Trust access policies (e.g., "/admin/*", "/hangfire").
    /// </summary>
    public List<string> ZeroTrustProtectedPaths { get; set; } = ["/admin/*", "/hangfire", "/dashboard/*"];

    /// <summary>
    /// Cloudflare Access application audience tag (AUD).
    /// </summary>
    public string? ZeroTrustPolicyAud { get; set; }

    /// <summary>
    /// Allowed email domains for Zero Trust access (e.g., "@thewatch.app").
    /// </summary>
    public List<string> ZeroTrustAllowedEmails { get; set; } = [];

    // ─── Item 140: Argo Tunnels ───

    public bool UseArgoTunnels { get; set; }

    /// <summary>
    /// Argo Tunnel ID (from cloudflared tunnel create).
    /// </summary>
    public string? ArgoTunnelId { get; set; }

    /// <summary>
    /// Argo Tunnel credential file path.
    /// </summary>
    public string? ArgoCredentialPath { get; set; }

    /// <summary>
    /// Service-to-hostname mappings for Argo ingress rules.
    /// Key: hostname (e.g., "api.thewatch.app"), Value: local origin (e.g., "http://localhost:8080").
    /// </summary>
    public Dictionary<string, string> ArgoIngressRules { get; set; } = [];
}

// ─── WAF Sub-Models ───

public class WafCustomRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public WafAction Action { get; set; } = WafAction.Block;
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}

public class WafRateLimitRule
{
    public string Name { get; set; } = string.Empty;
    public string PathPattern { get; set; } = string.Empty;
    public int RequestsPerPeriod { get; set; } = 100;
    public int PeriodSeconds { get; set; } = 60;
    public WafAction Action { get; set; } = WafAction.Challenge;
}

public enum WafAction
{
    Block,
    Challenge,
    JsChallenge,
    ManagedChallenge,
    Log,
    Skip
}
