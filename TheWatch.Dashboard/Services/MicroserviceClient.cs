using System.Net.Http.Json;
using TheWatch.Shared.Contracts;

namespace TheWatch.Dashboard.Services;

public class ProgramInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? ResponseTimeMs { get; set; }
    public DateTime? LastCheck { get; set; }
}

public class MicroserviceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    // Map program ID to Aspire resource name
    private static readonly Dictionary<string, (string ResourceName, ProgramInfo Info)> _programs = new()
    {
        ["P1"] = ("p1-coregateway", new() { Id = "P1", Name = "Core Gateway", Description = "Gateway and profiles", Icon = "dns" }),
        ["P2"] = ("p2-voiceemergency", new() { Id = "P2", Name = "Voice Emergency", Description = "Emergency reporting, dispatch, voice SOS", Icon = "emergency" }),
        ["P3"] = ("p3-meshnetwork", new() { Id = "P3", Name = "Mesh Network", Description = "BLE mesh for off-grid comms", Icon = "cell_tower" }),
        ["P4"] = ("p4-wearable", new() { Id = "P4", Name = "Wearable", Description = "Apple Watch, Wear OS, Garmin, Samsung, Fitbit", Icon = "watch" }),
        ["P5"] = ("p5-authsecurity", new() { Id = "P5", Name = "Auth & Security", Description = "JWT, MFA, STRIDE/MITRE threat intel", Icon = "shield" }),
        ["P6"] = ("p6-firstresponder", new() { Id = "P6", Name = "First Responder", Description = "Dispatch optimization", Icon = "local_police" }),
        ["P7"] = ("p7-familyhealth", new() { Id = "P7", Name = "Family Health", Description = "Child check-ins, vital signs, evidence chain", Icon = "favorite" }),
        ["P8"] = ("p8-disasterrelief", new() { Id = "P8", Name = "Disaster Relief", Description = "Resource matching, evacuation", Icon = "flood" }),
        ["P9"] = ("p9-doctorservices", new() { Id = "P9", Name = "Doctor Services", Description = "Doctor marketplace", Icon = "medical_services" }),
        ["P10"] = ("p10-gamification", new() { Id = "P10", Name = "Gamification", Description = "Rewards, ML training, geo-challenges", Icon = "trophy" }),
        ["P11"] = ("p11-surveillance", new() { Id = "P11", Name = "Surveillance", Description = "Camera registration, footage analysis, crime locations", Icon = "videocam" }),
        ["P12"] = ("p12-notifications", new() { Id = "P12", Name = "Notifications", Description = "Push, SMS, email, broadcast alerts", Icon = "notifications" })
    };

    public MicroserviceClient(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public IReadOnlyDictionary<string, ProgramInfo> Programs =>
        _programs.ToDictionary(p => p.Key, p => p.Value.Info);

    private string ResolveBaseUrl(string programId)
    {
        if (!_programs.TryGetValue(programId, out var entry))
            return $"http://localhost:5000";

        var resourceName = entry.ResourceName;

        // Try Aspire-injected service discovery config (services:{name}:http:0 or services:{name}:https:0)
        var httpUrl = _config[$"services:{resourceName}:http:0"];
        if (!string.IsNullOrEmpty(httpUrl))
            return httpUrl;

        var httpsUrl = _config[$"services:{resourceName}:https:0"];
        if (!string.IsNullOrEmpty(httpsUrl))
            return httpsUrl;

        // Fallback: use Aspire service discovery scheme (resolved by AddServiceDiscovery)
        return $"https+http://{resourceName}";
    }

    public async Task<ProgramInfo> CheckHealthAsync(string programId)
    {
        if (!_programs.TryGetValue(programId, out var entry))
            return new ProgramInfo { Id = programId, Status = "Unknown" };

        var info = entry.Info;
        var baseUrl = ResolveBaseUrl(programId);
        info.BaseUrl = baseUrl;

        try
        {
            var client = _httpClientFactory.CreateClient("microservices");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync($"{baseUrl}/health");
            sw.Stop();

            info.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
            info.LastCheck = DateTime.UtcNow;

            if (response.IsSuccessStatusCode)
            {
                var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
                info.Status = health?.Status ?? "Healthy";
            }
            else
            {
                info.Status = "Unhealthy";
            }
        }
        catch
        {
            info.Status = "Offline";
            info.ResponseTimeMs = null;
            info.LastCheck = DateTime.UtcNow;
        }

        return info;
    }

    public async Task<Dictionary<string, ProgramInfo>> CheckAllHealthAsync()
    {
        var tasks = _programs.Keys.Select(async id =>
        {
            var result = await CheckHealthAsync(id);
            return (id, result);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.id, r => r.result);
    }
}
