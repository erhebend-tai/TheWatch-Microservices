using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P1.CoreGateway.Core;
using Xunit;

namespace TheWatch.P1.CoreGateway.Tests;

public class CoreEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CoreEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<UserProfile> CreateProfileAsync(string name = "Test User", UserRole role = UserRole.Citizen)
    {
        var response = await _client.PostAsJsonAsync("/api/profiles",
            new CreateProfileRequest(name, $"{Guid.NewGuid():N}@test.com", "555-0100", role));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<UserProfile>())!;
    }

    [Fact]
    public async Task CreateProfile_ReturnsCreated()
    {
        var profile = await CreateProfileAsync("Jane Doe", UserRole.Responder);
        profile.DisplayName.Should().Be("Jane Doe");
        profile.Role.Should().Be(UserRole.Responder);
    }

    [Fact]
    public async Task GetProfile_ReturnsOk()
    {
        var created = await CreateProfileAsync();
        var response = await _client.GetAsync($"/api/profiles/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/profiles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListProfiles_ReturnsPaginated()
    {
        await CreateProfileAsync("User A");
        await CreateProfileAsync("User B");

        var result = await _client.GetFromJsonAsync<ProfileListResponse>("/api/profiles?page=1&pageSize=50");
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsUpdated()
    {
        var profile = await CreateProfileAsync();
        var response = await _client.PutAsJsonAsync($"/api/profiles/{profile.Id}",
            new UpdateProfileRequest(DisplayName: "Updated Name", Latitude: 33.45, Longitude: -112.07));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserProfile>();
        updated!.DisplayName.Should().Be("Updated Name");
        updated.Latitude.Should().Be(33.45);
    }

    [Fact]
    public async Task SetPreference_ReturnsProfile()
    {
        var profile = await CreateProfileAsync();
        var response = await _client.PutAsJsonAsync($"/api/profiles/{profile.Id}/preferences",
            new SetPreferenceRequest("theme", "dark"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserProfile>();
        updated!.Preferences.Should().ContainKey("theme");
    }

    [Fact]
    public async Task DeactivateProfile_ReturnsNoContent()
    {
        var profile = await CreateProfileAsync();
        var response = await _client.DeleteAsync($"/api/profiles/{profile.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetConfig_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/config",
            new SetConfigRequest("app.version", "1.0.0", "Current app version"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<PlatformConfig>();
        config!.Key.Should().Be("app.version");
    }

    [Fact]
    public async Task GetConfig_ReturnsOk()
    {
        await _client.PostAsJsonAsync("/api/config", new SetConfigRequest("test.key", "test.value"));

        var response = await _client.GetAsync("/api/config/test.key");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
