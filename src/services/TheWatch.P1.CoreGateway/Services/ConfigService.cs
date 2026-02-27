using System.Collections.Concurrent;
using TheWatch.P1.CoreGateway.Core;

namespace TheWatch.P1.CoreGateway.Services;

public interface IConfigService
{
    Task<PlatformConfig> SetAsync(SetConfigRequest request);
    Task<PlatformConfig?> GetAsync(string key);
    Task<List<PlatformConfig>> ListAllAsync();
    Task<ServiceHealthSummary> CheckAllServicesAsync(HttpClient httpClient);
    Task<ServiceHealthSummary> RunScheduledHealthCheckAsync();
}

public class ConfigService : IConfigService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, PlatformConfig> _configs = new();

    public ConfigService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private static readonly Dictionary<string, string> ServiceUrls = new()
    {
        ["P1"] = "http://localhost:5001",
        ["P2"] = "http://localhost:5002",
        ["P3"] = "http://localhost:5003",
        ["P4"] = "http://localhost:5004",
        ["P5"] = "http://localhost:5005",
        ["P6"] = "http://localhost:5006",
        ["P7"] = "http://localhost:5007",
        ["P8"] = "http://localhost:5008",
        ["P9"] = "http://localhost:5009",
        ["P10"] = "http://localhost:5010",
    };

    public Task<PlatformConfig> SetAsync(SetConfigRequest request)
    {
        var config = new PlatformConfig
        {
            Key = request.Key,
            Value = request.Value,
            Description = request.Description,
            UpdatedAt = DateTime.UtcNow
        };

        _configs[request.Key] = config;
        return Task.FromResult(config);
    }

    public Task<PlatformConfig?> GetAsync(string key)
    {
        _configs.TryGetValue(key, out var config);
        return Task.FromResult(config);
    }

    public Task<List<PlatformConfig>> ListAllAsync()
    {
        return Task.FromResult(_configs.Values.OrderBy(c => c.Key).ToList());
    }

    public async Task<ServiceHealthSummary> CheckAllServicesAsync(HttpClient httpClient)
    {
        var services = new List<ServiceRegistration>();
        var healthy = 0;
        var unhealthy = 0;

        foreach (var (id, url) in ServiceUrls)
        {
            var reg = new ServiceRegistration
            {
                ServiceId = id,
                Name = $"TheWatch.{id}",
                BaseUrl = url,
                LastCheckedAt = DateTime.UtcNow
            };

            try
            {
                var response = await httpClient.GetAsync($"{url}/health");
                reg.Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy";
            }
            catch
            {
                reg.Status = "Offline";
            }

            if (reg.Status == "Healthy") healthy++;
            else unhealthy++;

            services.Add(reg);
        }

        return new ServiceHealthSummary(services, healthy, unhealthy);
    }

    public async Task<ServiceHealthSummary> RunScheduledHealthCheckAsync()
    {
        var client = _httpClientFactory.CreateClient("services");
        return await CheckAllServicesAsync(client);
    }
}
