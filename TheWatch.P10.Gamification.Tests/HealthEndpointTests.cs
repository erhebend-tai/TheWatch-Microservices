using Xunit;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.Shared.Contracts;

namespace TheWatch.P10.Gamification.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("Healthy");
        health.Program.Should().Be("P10");
    }

    [Fact]
    public async Task Info_ReturnsServiceMetadata()
    {
        var response = await _client.GetAsync("/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
