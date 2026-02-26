using System.Net.Http.Json;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Services;

public class WatchApiClient
{
    private readonly HttpClient _http;

    // Service base URLs — configurable at runtime
    private string P1 => "http://localhost:5001";
    private string P2 => "http://localhost:5002";
    private string P5 => "http://localhost:5005";
    private string P6 => "http://localhost:5006";
    private string P7 => "http://localhost:5007";
    private string P8 => "http://localhost:5008";

    private readonly Dictionary<string, string> _serviceUrls = new()
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

    public WatchApiClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    // === Health Check ===
    public async Task<bool> CheckHealthAsync(string program)
    {
        if (!_serviceUrls.TryGetValue(program, out var url)) return false;
        try
        {
            var response = await _http.GetAsync($"{url}/health");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // === P2 Voice Emergency ===
    public async Task<IncidentDto?> CreateIncidentAsync(CreateIncidentRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{P2}/api/incidents", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IncidentDto>();
    }

    public async Task<List<IncidentDto>> GetRecentIncidentsAsync(int pageSize = 5)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<IncidentListResponse>(
                $"{P2}/api/incidents?pageSize={pageSize}");
            return result?.Items ?? [];
        }
        catch { return []; }
    }

    public async Task CreateDispatchAsync(CreateDispatchRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{P2}/api/dispatch", request);
        response.EnsureSuccessStatusCode();
    }

    // === P7 Family Health ===
    public async Task<FamilyGroupDto?> GetFamilyGroupAsync()
    {
        try
        {
            var groups = await _http.GetFromJsonAsync<List<FamilyGroupDto>>($"{P7}/api/families");
            return groups?.FirstOrDefault();
        }
        catch { return null; }
    }

    public async Task CreateFamilyGroupAsync(string name)
    {
        var response = await _http.PostAsJsonAsync($"{P7}/api/families",
            new { Name = name });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<VitalReadingDto>> GetMemberVitalsAsync(Guid memberId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<VitalHistory>(
                $"{P7}/api/vitals/{memberId}?count=10");
            return result?.Readings ?? [];
        }
        catch { return []; }
    }

    public async Task<CheckInDto?> GetLatestCheckInAsync(Guid memberId)
    {
        try
        {
            var checkIns = await _http.GetFromJsonAsync<List<CheckInDto>>(
                $"{P7}/api/checkins/{memberId}");
            return checkIns?.FirstOrDefault();
        }
        catch { return null; }
    }

    public async Task CreateCheckInAsync(CheckInStatus status, string? message = null)
    {
        var request = new CreateCheckInRequest(status, message);
        var response = await _http.PostAsJsonAsync($"{P7}/api/checkins", request);
        response.EnsureSuccessStatusCode();
    }

    // === P5 Auth (used for profile) ===
    public async Task<UserInfoDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<UserInfoDto>($"{P5}/api/auth/me");
        }
        catch { return null; }
    }

    // Local DTO for vital history response
    private record VitalHistory(List<VitalReadingDto> Readings, int TotalCount);
}
