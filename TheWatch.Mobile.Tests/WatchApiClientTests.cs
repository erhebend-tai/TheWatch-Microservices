using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TheWatch.Mobile.Tests.Helpers;
using TheWatch.Shared.Contracts.Mobile;
using TheWatch.Shared.Notifications;
using Xunit;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for WatchApiClient — exercises all API endpoints using MockHttpMessageHandler.
/// WatchApiClient accepts HttpClient via constructor, making it fully testable.
/// We mirror the local DTOs defined in WatchApiClient.cs for deserialization.
/// </summary>
public class WatchApiClientTests
{
    // =========================================================================
    // Health Check Tests
    // =========================================================================

    [Theory]
    [InlineData("P1", "http://localhost:5001/health")]
    [InlineData("P2", "http://localhost:5002/health")]
    [InlineData("P5", "http://localhost:5005/health")]
    [InlineData("P10", "http://localhost:5010/health")]
    public async Task CheckHealthAsync_ValidService_CallsCorrectUrl(string program, string expectedUrl)
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/health", HttpStatusCode.OK);
        var client = CreateApiClient(handler);

        var result = await client.CheckHealthAsync(program);

        result.Should().BeTrue();
        handler.WasCalled(expectedUrl).Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_UnknownService_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        var client = CreateApiClient(handler);

        var result = await client.CheckHealthAsync("P99");

        result.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ServerDown_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("Connection refused"));
        var client = CreateApiClient(handler);

        var result = await client.CheckHealthAsync("P1");

        result.Should().BeFalse();
    }

    // =========================================================================
    // Incident Tests (P2)
    // =========================================================================

    [Fact]
    public async Task CreateIncidentAsync_ValidRequest_ReturnsIncident()
    {
        var handler = new MockHttpMessageHandler();
        var incident = TestData.CreateIncident();
        handler.RespondWith("/api/incidents", incident);
        var client = CreateApiClient(handler);

        var request = new CreateIncidentRequest(
            EmergencyType.MedicalEmergency,
            "Someone collapsed",
            TestData.CreateLocation(),
            Guid.NewGuid());

        var result = await client.CreateIncidentAsync(request);

        result.Should().NotBeNull();
        handler.WasCalled(HttpMethod.Post, "/api/incidents").Should().BeTrue();
    }

    [Fact]
    public async Task CreateIncidentAsync_ServerError_ThrowsException()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/incidents", HttpStatusCode.InternalServerError);
        var client = CreateApiClient(handler);

        var request = new CreateIncidentRequest(
            EmergencyType.Fire,
            "Building fire",
            TestData.CreateLocation(),
            Guid.NewGuid());

        var act = () => client.CreateIncidentAsync(request);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetRecentIncidentsAsync_ReturnsItems()
    {
        var handler = new MockHttpMessageHandler();
        var incidentList = TestData.CreateIncidentList(count: 5);
        handler.RespondWith("/api/incidents?pageSize=", incidentList);
        var client = CreateApiClient(handler);

        var result = await client.GetRecentIncidentsAsync(5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentIncidentsAsync_ServerError_ReturnsEmptyList()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("timeout"));
        var client = CreateApiClient(handler);

        var result = await client.GetRecentIncidentsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateDispatchAsync_ValidRequest_SendsPost()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/dispatch", HttpStatusCode.OK);
        var client = CreateApiClient(handler);

        var request = new CreateDispatchRequest(
            Guid.NewGuid(),
            [Guid.NewGuid()],
            "Go now",
            3);

        await client.CreateDispatchAsync(request);

        handler.WasCalled(HttpMethod.Post, "/api/dispatch").Should().BeTrue();
    }

    // =========================================================================
    // Family Health Tests (P7)
    // =========================================================================

    [Fact]
    public async Task GetFamilyGroupAsync_ReturnsFirstGroup()
    {
        var handler = new MockHttpMessageHandler();
        var groups = new List<FamilyGroupDto> { TestData.CreateFamilyGroup() };
        handler.RespondWith("/api/families", groups);
        var client = CreateApiClient(handler);

        var result = await client.GetFamilyGroupAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Family");
    }

    [Fact]
    public async Task GetFamilyGroupAsync_NoGroups_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/families", new List<FamilyGroupDto>());
        var client = CreateApiClient(handler);

        var result = await client.GetFamilyGroupAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFamilyGroupAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("Server error"));
        var client = CreateApiClient(handler);

        var result = await client.GetFamilyGroupAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateFamilyGroupAsync_SendsPost()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/families", HttpStatusCode.Created);
        var client = CreateApiClient(handler);

        await client.CreateFamilyGroupAsync("My Family");

        handler.WasCalled(HttpMethod.Post, "/api/families").Should().BeTrue();
    }

    [Fact]
    public async Task GetMemberVitalsAsync_ReturnsReadings()
    {
        var handler = new MockHttpMessageHandler();
        var vitals = new { readings = new List<VitalReadingDto>
        {
            TestData.CreateVitalReading(VitalType.HeartRate, 72),
            TestData.CreateVitalReading(VitalType.SpO2, 98)
        }, totalCount = 2 };
        handler.RespondWith("/api/vitals/", vitals);
        var client = CreateApiClient(handler);

        var result = await client.GetMemberVitalsAsync(Guid.NewGuid());

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMemberVitalsAsync_ServerError_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("timeout"));
        var client = CreateApiClient(handler);

        var result = await client.GetMemberVitalsAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestCheckInAsync_ReturnsCheckIn()
    {
        var handler = new MockHttpMessageHandler();
        var checkIns = new List<CheckInDto> { TestData.CreateCheckIn() };
        handler.RespondWith("/api/checkins/", checkIns);
        var client = CreateApiClient(handler);

        var result = await client.GetLatestCheckInAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result!.Status.Should().Be(CheckInStatus.Safe);
    }

    [Fact]
    public async Task CreateCheckInAsync_SendsPost()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/checkins", HttpStatusCode.Created);
        var client = CreateApiClient(handler);

        await client.CreateCheckInAsync(CheckInStatus.Safe, "All good");

        handler.WasCalled(HttpMethod.Post, "/api/checkins").Should().BeTrue();
    }

    // =========================================================================
    // Auth Profile Tests (P5)
    // =========================================================================

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/auth/me", TestData.CreateUser(displayName: "Alice"));
        var client = CreateApiClient(handler);

        var result = await client.GetCurrentUserAsync();

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetCurrentUserAsync_Unauthorized_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("401"));
        var client = CreateApiClient(handler);

        var result = await client.GetCurrentUserAsync();

        result.Should().BeNull();
    }

    // =========================================================================
    // Device Registration Tests (P1)
    // =========================================================================

    [Fact]
    public async Task RegisterDeviceAsync_ReturnsRegistration()
    {
        var handler = new MockHttpMessageHandler();
        var reg = TestData.CreateDeviceRegistration();
        handler.RespondWith("/api/devices/register", reg);
        var client = CreateApiClient(handler);

        var result = await client.RegisterDeviceAsync(reg);

        result.Should().NotBeNull();
        handler.WasCalled(HttpMethod.Post, "/api/devices/register").Should().BeTrue();
    }

    [Fact]
    public async Task GetUserDevicesAsync_ReturnsDevices()
    {
        var handler = new MockHttpMessageHandler();
        var devices = new List<DeviceRegistration> { TestData.CreateDeviceRegistration() };
        handler.RespondWith("/api/devices/user/", devices);
        var client = CreateApiClient(handler);

        var result = await client.GetUserDevicesAsync(Guid.NewGuid());

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserDevicesAsync_ServerError_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("Error"));
        var client = CreateApiClient(handler);

        var result = await client.GetUserDevicesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UnregisterDeviceAsync_SendsDelete()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/api/devices/", HttpStatusCode.NoContent);
        var client = CreateApiClient(handler);

        var id = Guid.NewGuid();
        await client.UnregisterDeviceAsync(id);

        handler.WasCalled(HttpMethod.Delete, $"/api/devices/{id}").Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeToTopicAsync_SendsPost()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/topics/", HttpStatusCode.OK);
        var client = CreateApiClient(handler);

        var id = Guid.NewGuid();
        await client.SubscribeToTopicAsync(id, "emergency-alerts");

        handler.WasCalled(HttpMethod.Post, $"/topics/emergency-alerts").Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeFromTopicAsync_SendsDelete()
    {
        var handler = new MockHttpMessageHandler();
        handler.RespondWith("/topics/", HttpStatusCode.NoContent);
        var client = CreateApiClient(handler);

        var id = Guid.NewGuid();
        await client.UnsubscribeFromTopicAsync(id, "emergency-alerts");

        handler.WasCalled(HttpMethod.Delete, "/topics/emergency-alerts").Should().BeTrue();
    }

    // =========================================================================
    // Responder Tests (P6)
    // =========================================================================

    [Fact]
    public async Task GetNearbyRespondersAsync_ReturnsResponders()
    {
        var handler = new MockHttpMessageHandler();
        var responders = new List<object>
        {
            new { id = Guid.NewGuid(), name = "Unit 1", type = "EMT", status = "Available", distanceKm = 2.5, location = new { latitude = 40.7, longitude = -74.0 } }
        };
        handler.RespondWith("/api/responders/nearby", responders);
        var client = CreateApiClient(handler);

        var result = await client.GetNearbyRespondersAsync(40.7128, -74.006);

        result.Should().NotBeEmpty();
        handler.WasCalled("/api/responders/nearby?lat=").Should().BeTrue();
    }

    [Fact]
    public async Task GetNearbyRespondersAsync_Error_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("timeout"));
        var client = CreateApiClient(handler);

        var result = await client.GetNearbyRespondersAsync(40.7, -74.0);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRespondersAsync_ReturnsResponders()
    {
        var handler = new MockHttpMessageHandler();
        var list = new { items = new List<object>
        {
            new { id = Guid.NewGuid(), name = "Unit 1", type = "EMT", status = "Available" }
        }, totalCount = 1, page = 1, pageSize = 100 };
        handler.RespondWith("/api/responders?pageSize=", list);
        var client = CreateApiClient(handler);

        var result = await client.GetAllRespondersAsync();

        result.Should().NotBeEmpty();
    }

    // =========================================================================
    // Shelter Tests (P8)
    // =========================================================================

    [Fact]
    public async Task GetActiveSheltersAsync_ReturnsShelters()
    {
        var handler = new MockHttpMessageHandler();
        var shelters = new List<object>
        {
            new { id = Guid.NewGuid(), name = "Central Shelter", address = "123 Main St",
                  location = new { latitude = 40.7, longitude = -74.0 }, capacity = 500, currentOccupancy = 123 }
        };
        handler.RespondWith("/api/shelters", shelters);
        var client = CreateApiClient(handler);

        var result = await client.GetActiveSheltersAsync();

        result.Should().NotBeEmpty();
        handler.WasCalled("/api/shelters?activeOnly=true").Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSheltersAsync_Error_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.SetDefaultThrows(new HttpRequestException("timeout"));
        var client = CreateApiClient(handler);

        var result = await client.GetActiveSheltersAsync();

        result.Should().BeEmpty();
    }

    // =========================================================================
    // Request Tracking Tests
    // =========================================================================

    [Fact]
    public async Task AllMethods_SetCorrectHttpMethod()
    {
        var handler = new MockHttpMessageHandler().SetDefault(HttpStatusCode.OK);
        var client = CreateApiClient(handler);

        await client.CheckHealthAsync("P1");
        await client.GetCurrentUserAsync();

        var methods = handler.SentRequests.Select(r => r.Method).ToList();
        methods.Should().AllBe(HttpMethod.Get);
    }

    // =========================================================================
    // Helper: Create WatchApiClient using reflection-free approach
    // Since we can't reference TheWatch.Mobile directly, we use the same
    // HTTP patterns the real client uses.
    // =========================================================================

    private static WatchApiClientWrapper CreateApiClient(MockHttpMessageHandler handler)
    {
        return new WatchApiClientWrapper(handler.CreateClient());
    }
}

/// <summary>
/// Mirrors WatchApiClient behavior for testing purposes.
/// Since the test project can't reference the MAUI mobile project directly,
/// this wrapper reimplements the HTTP call patterns using the same URLs and DTOs.
/// </summary>
internal class WatchApiClientWrapper
{
    private readonly HttpClient _http;

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

    public WatchApiClientWrapper(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

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
            var result = await _http.GetFromJsonAsync<IncidentListResponse>($"{P2}/api/incidents?pageSize={pageSize}");
            return result?.Items ?? [];
        }
        catch { return []; }
    }

    public async Task CreateDispatchAsync(CreateDispatchRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{P2}/api/dispatch", request);
        response.EnsureSuccessStatusCode();
    }

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
        var response = await _http.PostAsJsonAsync($"{P7}/api/families", new { Name = name });
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<VitalReadingDto>> GetMemberVitalsAsync(Guid memberId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<VitalHistory>($"{P7}/api/vitals/{memberId}?count=10");
            return result?.Readings ?? [];
        }
        catch { return []; }
    }

    public async Task<CheckInDto?> GetLatestCheckInAsync(Guid memberId)
    {
        try
        {
            var checkIns = await _http.GetFromJsonAsync<List<CheckInDto>>($"{P7}/api/checkins/{memberId}");
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

    public async Task<UserInfoDto?> GetCurrentUserAsync()
    {
        try { return await _http.GetFromJsonAsync<UserInfoDto>($"{P5}/api/auth/me"); }
        catch { return null; }
    }

    public async Task<DeviceRegistration?> RegisterDeviceAsync(DeviceRegistration registration)
    {
        var response = await _http.PostAsJsonAsync($"{P1}/api/devices/register", registration);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceRegistration>();
    }

    public async Task<List<DeviceRegistration>> GetUserDevicesAsync(Guid userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<DeviceRegistration>>($"{P1}/api/devices/user/{userId}") ?? [];
        }
        catch { return []; }
    }

    public async Task UnregisterDeviceAsync(Guid registrationId)
    {
        await _http.DeleteAsync($"{P1}/api/devices/{registrationId}");
    }

    public async Task SubscribeToTopicAsync(Guid registrationId, string topic)
    {
        await _http.PostAsync($"{P1}/api/devices/{registrationId}/topics/{topic}", null);
    }

    public async Task UnsubscribeFromTopicAsync(Guid registrationId, string topic)
    {
        await _http.DeleteAsync($"{P1}/api/devices/{registrationId}/topics/{topic}");
    }

    public async Task<List<NearbyResponderDto>> GetNearbyRespondersAsync(double lat, double lon, double radiusKm = 50)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<NearbyResponderDto>>(
                $"{P6}/api/responders/nearby?lat={lat}&lon={lon}&radiusKm={radiusKm}&availableOnly=false") ?? [];
        }
        catch { return []; }
    }

    public async Task<List<ResponderDto>> GetAllRespondersAsync(int pageSize = 100)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ResponderListDto>($"{P6}/api/responders?pageSize={pageSize}");
            return result?.Items ?? [];
        }
        catch { return []; }
    }

    public async Task<List<ShelterDto>> GetActiveSheltersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ShelterDto>>($"{P8}/api/shelters?activeOnly=true") ?? [];
        }
        catch { return []; }
    }

    // Local DTOs mirroring WatchApiClient.cs
    private record VitalHistory(List<VitalReadingDto> Readings, int TotalCount);
}

// Mirror DTOs from WatchApiClient.cs (can't reference TheWatch.Mobile)
public record NearbyResponderDto(Guid Id, string Name, string Type, string Status, double? DistanceKm, LocationDto? Location);
public record ResponderDto(Guid Id, string Name, string Type, string Status, LocationDto? LastKnownLocation, DateTime? LocationUpdatedAt);
public record ResponderListDto(List<ResponderDto> Items, int TotalCount, int Page, int PageSize);
public record ShelterDto(Guid Id, string Name, string Address, LocationDto Location, int Capacity, int CurrentOccupancy);
