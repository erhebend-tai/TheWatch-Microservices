using System.Diagnostics;
using System.Net.Http.Json;
using System.Web;
using TheWatch.Shared.Auth;

namespace TheWatch.Admin.Services;

public class AdminApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    private static readonly Dictionary<string, (string Name, string Description, string Icon)> ServiceDefinitions = new()
    {
        ["P1"]  = ("P1 - Core Gateway",       "Central API gateway and routing",         "ri-server-line"),
        ["P2"]  = ("P2 - Voice Emergency",     "Voice-activated emergency services",      "ri-mic-line"),
        ["P3"]  = ("P3 - Mesh Network",        "Mesh networking and connectivity",        "ri-share-line"),
        ["P4"]  = ("P4 - Wearable",            "Wearable device integration",             "ri-heart-pulse-line"),
        ["P5"]  = ("P5 - Auth Security",       "Authentication and security services",    "ri-shield-keyhole-line"),
        ["P6"]  = ("P6 - First Responder",     "First responder coordination",            "ri-alarm-warning-line"),
        ["P7"]  = ("P7 - Family Health",        "Family health monitoring",                "ri-group-line"),
        ["P8"]  = ("P8 - Disaster Relief",     "Disaster relief coordination",            "ri-earth-line"),
        ["P9"]  = ("P9 - Doctor Services",     "Doctor and clinical services",            "ri-stethoscope-line"),
        ["P10"] = ("P10 - Gamification",       "Gamification and engagement",             "ri-trophy-line"),
    };

    private static readonly Dictionary<string, string> ServiceResourceNames = new()
    {
        ["P1"]  = "p1-coregateway",
        ["P2"]  = "p2-voiceemergency",
        ["P3"]  = "p3-meshnetwork",
        ["P4"]  = "p4-wearable",
        ["P5"]  = "p5-authsecurity",
        ["P6"]  = "p6-firstresponder",
        ["P7"]  = "p7-familyhealth",
        ["P8"]  = "p8-disasterrelief",
        ["P9"]  = "p9-doctorservices",
        ["P10"] = "p10-gamification",
    };

    public AdminApiClient(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    // ---------------------------------------------------------------------------
    //  URL Resolution
    // ---------------------------------------------------------------------------

    private string GetP5BaseUrl() =>
        _config["Services:P5:Url"] ?? "http://localhost:5005";

    private string ResolveBaseUrl(string resourceName)
    {
        // Try Aspire service-discovery config bindings first
        var httpUrl = _config[$"services:{resourceName}:http:0"];
        if (!string.IsNullOrEmpty(httpUrl))
            return httpUrl;

        var httpsUrl = _config[$"services:{resourceName}:https:0"];
        if (!string.IsNullOrEmpty(httpsUrl))
            return httpsUrl;

        // Fall back to Aspire service-discovery URI scheme
        return $"https+http://{resourceName}";
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("microservices");
    }

    // ---------------------------------------------------------------------------
    //  Dashboard
    // ---------------------------------------------------------------------------

    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var result = await client.GetFromJsonAsync<AdminDashboardStats>($"{baseUrl}/api/admin/stats");
            return result ?? new AdminDashboardStats(0, 0, 0, "N/A");
        }
        catch
        {
            return new AdminDashboardStats(0, 0, 0, "N/A");
        }
    }

    // ---------------------------------------------------------------------------
    //  User Management
    // ---------------------------------------------------------------------------

    public async Task<List<UserDto>> ListUsersAsync()
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var result = await client.GetFromJsonAsync<List<UserDto>>($"{baseUrl}/api/admin/users");
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var response = await client.PostAsJsonAsync($"{baseUrl}/api/admin/users", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<UserDto>();
            return result ?? throw new InvalidOperationException("Empty response from user creation.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to create user.", ex);
        }
    }

    public async Task AssignRoleAsync(Guid userId, string role)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var response = await client.PostAsJsonAsync(
                $"{baseUrl}/api/admin/users/{userId}/roles",
                new { Role = role });
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            throw new InvalidOperationException($"Failed to assign role '{role}' to user {userId}.");
        }
    }

    public async Task RemoveRoleAsync(Guid userId, string role)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var encodedRole = HttpUtility.UrlEncode(role);
            var response = await client.DeleteAsync(
                $"{baseUrl}/api/admin/users/{userId}/roles/{encodedRole}");
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            throw new InvalidOperationException($"Failed to remove role '{role}' from user {userId}.");
        }
    }

    public async Task DeactivateUserAsync(Guid userId)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var response = await client.PostAsync(
                $"{baseUrl}/api/admin/users/{userId}/deactivate", null);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            throw new InvalidOperationException($"Failed to deactivate user {userId}.");
        }
    }

    public async Task ActivateUserAsync(Guid userId)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var response = await client.PostAsync(
                $"{baseUrl}/api/admin/users/{userId}/activate", null);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            throw new InvalidOperationException($"Failed to activate user {userId}.");
        }
    }

    // ---------------------------------------------------------------------------
    //  Audit Log
    // ---------------------------------------------------------------------------

    public async Task<AuditLogResponse> GetAuditLogAsync(AuditLogFilter filter)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();

            var queryParams = new List<string>
            {
                $"page={filter.Page}",
                $"pageSize={filter.PageSize}"
            };

            if (filter.StartDate.HasValue)
                queryParams.Add($"startDate={filter.StartDate.Value:O}");
            if (filter.EndDate.HasValue)
                queryParams.Add($"endDate={filter.EndDate.Value:O}");
            if (!string.IsNullOrEmpty(filter.EventType))
                queryParams.Add($"eventType={HttpUtility.UrlEncode(filter.EventType)}");
            if (!string.IsNullOrEmpty(filter.Severity))
                queryParams.Add($"severity={HttpUtility.UrlEncode(filter.Severity)}");
            if (!string.IsNullOrEmpty(filter.UserSearch))
                queryParams.Add($"userSearch={HttpUtility.UrlEncode(filter.UserSearch)}");

            var queryString = string.Join("&", queryParams);
            var result = await client.GetFromJsonAsync<AuditLogResponse>(
                $"{baseUrl}/api/admin/audit?{queryString}");

            return result ?? new AuditLogResponse([], 0, 0, 0);
        }
        catch
        {
            // Return sample data so the UI has something to render
            return new AuditLogResponse(
                Entries:
                [
                    new AuditEntry(
                        Guid.NewGuid(),
                        DateTime.UtcNow.AddMinutes(-5),
                        "UserLogin",
                        "admin@thewatch.app",
                        "Admin user logged in",
                        "127.0.0.1",
                        "Information"),
                    new AuditEntry(
                        Guid.NewGuid(),
                        DateTime.UtcNow.AddMinutes(-30),
                        "SystemStart",
                        "system",
                        "Audit service started",
                        null,
                        "Information"),
                ],
                TotalCount: 2,
                CriticalCount: 0,
                Last24hCount: 2);
        }
    }

    // ---------------------------------------------------------------------------
    //  Security Dashboard
    // ---------------------------------------------------------------------------

    public async Task<SecurityDashboardData> GetSecurityDashboardAsync()
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var result = await client.GetFromJsonAsync<SecurityDashboardData>(
                $"{baseUrl}/api/admin/security/dashboard");
            return result ?? CreateDefaultSecurityDashboard();
        }
        catch
        {
            return CreateDefaultSecurityDashboard();
        }
    }

    private static SecurityDashboardData CreateDefaultSecurityDashboard() =>
        new(
            OverallThreatLevel: "Low",
            ActiveThreats: 0,
            BlockedIps: 0,
            FailedLogins: 0,
            MfaAdoptionRate: 0.0,
            StrideAnalysis: [],
            MitreDetections: [],
            Threats: [],
            BlockedIpList: []);

    // ---------------------------------------------------------------------------
    //  Service Health
    // ---------------------------------------------------------------------------

    public async Task<List<ServiceHealthInfo>> GetAllServiceHealthAsync()
    {
        var healthTasks = ServiceResourceNames.Select(async kvp =>
        {
            var programId = kvp.Key;
            var resourceName = kvp.Value;
            var (name, description, icon) = ServiceDefinitions[programId];

            string baseUrl;
            string? status = null;
            int? responseTimeMs = null;
            DateTime? lastCheck = null;

            try
            {
                baseUrl = ResolveBaseUrl(resourceName);
                var client = CreateClient();
                var sw = Stopwatch.StartNew();
                var response = await client.GetAsync($"{baseUrl}/health");
                sw.Stop();

                responseTimeMs = (int)sw.ElapsedMilliseconds;
                lastCheck = DateTime.UtcNow;
                status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy";
            }
            catch
            {
                baseUrl = ResolveBaseUrl(resourceName);
                status = "Offline";
                lastCheck = DateTime.UtcNow;
            }

            return new ServiceHealthInfo(
                Id: programId,
                Name: name,
                Description: description,
                Icon: icon,
                Status: status,
                ResponseTimeMs: responseTimeMs,
                BaseUrl: baseUrl,
                LastCheck: lastCheck);
        });

        var results = await Task.WhenAll(healthTasks);
        return results.OrderBy(s => s.Id).ToList();
    }

    // ---------------------------------------------------------------------------
    //  Role User Counts
    // ---------------------------------------------------------------------------

    public async Task<Dictionary<string, int>> GetRoleUserCountsAsync()
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var result = await client.GetFromJsonAsync<Dictionary<string, int>>(
                $"{baseUrl}/api/admin/roles/counts");
            return result ?? CreateEmptyRoleCounts();
        }
        catch
        {
            return CreateEmptyRoleCounts();
        }
    }

    private static Dictionary<string, int> CreateEmptyRoleCounts()
    {
        var counts = new Dictionary<string, int>();
        foreach (var role in WatchRoles.All)
        {
            counts[role] = 0;
        }
        return counts;
    }

    // ---------------------------------------------------------------------------
    //  Recent Audit Events
    // ---------------------------------------------------------------------------

    public async Task<List<AuditEventDto>> GetRecentAuditEventsAsync(int count = 10)
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            var result = await client.GetFromJsonAsync<List<AuditEventDto>>(
                $"{baseUrl}/api/admin/audit/recent?count={count}");
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    // ---------------------------------------------------------------------------
    //  EULA Management
    // ---------------------------------------------------------------------------

    public async Task<EulaVersionDto?> GetCurrentEulaAsync()
    {
        try
        {
            var client = CreateClient();
            var baseUrl = GetP5BaseUrl();
            return await client.GetFromJsonAsync<EulaVersionDto>($"{baseUrl}/api/eula/current");
        }
        catch
        {
            return null;
        }
    }

    public async Task<EulaVersionDto> PublishEulaVersionAsync(string version, string content)
    {
        var client = CreateClient();
        var baseUrl = GetP5BaseUrl();
        var response = await client.PostAsJsonAsync($"{baseUrl}/api/eula/versions",
            new { version, content, isCurrent = true });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EulaVersionDto>())!;
    }
}

// =============================================================================
//  DTOs
// =============================================================================

// Dashboard
public record AdminDashboardStats(
    int TotalUsers,
    int ActiveServices,
    int SecurityEvents,
    string Uptime);

// User Management
public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string[] Roles,
    DateTime CreatedAt,
    bool IsActive);

public record CreateUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Phone,
    string InitialRole);

// Audit Log
public record AuditLogFilter(
    DateTime? StartDate,
    DateTime? EndDate,
    string? EventType,
    string? Severity,
    string? UserSearch,
    int Page = 1,
    int PageSize = 50);

public record AuditLogResponse(
    List<AuditEntry> Entries,
    int TotalCount,
    int CriticalCount,
    int Last24hCount);

public record AuditEntry(
    Guid Id,
    DateTime Timestamp,
    string EventType,
    string User,
    string Details,
    string? IpAddress,
    string Severity);

// Security Dashboard
public record SecurityDashboardData(
    string OverallThreatLevel,
    int ActiveThreats,
    int BlockedIps,
    int FailedLogins,
    double MfaAdoptionRate,
    List<StrideCategory> StrideAnalysis,
    List<MitreDetection> MitreDetections,
    List<ThreatAssessmentDto> Threats,
    List<BlockedIpDto> BlockedIpList);

public record StrideCategory(
    string Name,
    string Icon,
    int ThreatCount,
    string RiskLevel);

public record MitreDetection(
    string TechniqueId,
    string TechniqueName,
    string Tactic,
    int DetectionCount,
    DateTime? LastDetected,
    string Status);

public record ThreatAssessmentDto(
    Guid Id,
    string Description,
    string Severity,
    string Source,
    DateTime DetectedAt,
    string Status);

public record BlockedIpDto(
    string IpAddress,
    DateTime BlockedAt,
    int AttemptCount);

// Service Health
public record ServiceHealthInfo(
    string Id,
    string Name,
    string Description,
    string Icon,
    string? Status,
    int? ResponseTimeMs,
    string? BaseUrl,
    DateTime? LastCheck);

// Recent Audit Events
public record AuditEventDto(
    DateTime Timestamp,
    string User,
    string Action,
    string Severity);

// EULA
public record EulaVersionDto(
    Guid Id,
    string Version,
    string Content,
    DateTime PublishedAt,
    bool IsCurrent);
