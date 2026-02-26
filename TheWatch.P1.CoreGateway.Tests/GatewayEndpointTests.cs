using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P1.CoreGateway.Core;
using TheWatch.P1.CoreGateway.Models;
using Xunit;

namespace TheWatch.P1.CoreGateway.Tests;

public class GatewayEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<UserProfile> CreateProfileAsync(string name = "Test User")
    {
        var response = await _client.PostAsJsonAsync("/api/profiles",
            new CreateProfileRequest(name, $"{Guid.NewGuid():N}@test.com", "555-0100"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<UserProfile>())!;
    }

    [Fact]
    public async Task CreateProfile_ReturnsCreated()
    {
        var profile = await CreateProfileAsync("John Doe");
        profile.DisplayName.Should().Be("John Doe");
        profile.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProfile_Existing_ReturnsOk()
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
    public async Task UpdateProfile_ReturnsUpdated()
    {
        var profile = await CreateProfileAsync();
        var response = await _client.PutAsJsonAsync($"/api/profiles/{profile.Id}",
            new UpdateProfileRequest("Updated Name", "555-9999"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserProfile>();
        updated!.DisplayName.Should().Be("Updated Name");
        updated.Phone.Should().Be("555-9999");
    }

    [Fact]
    public async Task DeleteProfile_ReturnsNoContent()
    {
        var profile = await CreateProfileAsync();
        var response = await _client.DeleteAsync($"/api/profiles/{profile.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetConfig_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/config",
            new SetConfigRequest("app.name", "TheWatch", "Application name"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<PlatformConfig>();
        config!.Key.Should().Be("app.name");
        config.Value.Should().Be("TheWatch");
    }

    [Fact]
    public async Task GetConfig_Existing_ReturnsOk()
    {
        await _client.PostAsJsonAsync("/api/config", new SetConfigRequest("test.key", "test.value"));

        var response = await _client.GetAsync("/api/config/test.key");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetServiceHealth_ReturnsSummary()
    {
        var response = await _client.GetFromJsonAsync<ServiceHealthSummary>("/api/services/health");
        response!.Services.Count.Should().Be(10);
        response.HealthyCount.Should().BeGreaterThanOrEqualTo(0);
    }
}
