using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P2.VoiceEmergency.Emergency;
using Xunit;

namespace TheWatch.P2.VoiceEmergency.Tests;

public class IncidentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IncidentEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Incident> CreateTestIncidentAsync(EmergencyType type = EmergencyType.MedicalEmergency)
    {
        var request = new CreateIncidentRequest(
            Type: type,
            Description: "Test incident for integration testing",
            Location: new Location(33.4484, -112.0740),
            ReporterId: Guid.NewGuid(),
            ReporterName: "Test Reporter",
            ReporterPhone: "+1555000222",
            Severity: 4,
            Tags: ["test", "integration"]);

        var response = await _client.PostAsJsonAsync("/api/incidents", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var incident = await response.Content.ReadFromJsonAsync<Incident>();
        incident.Should().NotBeNull();
        return incident!;
    }

    [Fact]
    public async Task CreateIncident_ReturnsCreatedWithId()
    {
        var incident = await CreateTestIncidentAsync();

        incident.Id.Should().NotBeEmpty();
        incident.Type.Should().Be(EmergencyType.MedicalEmergency);
        incident.Description.Should().Contain("Test incident");
        incident.Status.Should().Be(IncidentStatus.Reported);
        incident.Severity.Should().Be(4);
        incident.Location.Latitude.Should().BeApproximately(33.4484, 0.001);
    }

    [Fact]
    public async Task GetIncident_ExistingId_ReturnsIncident()
    {
        var created = await CreateTestIncidentAsync();

        var response = await _client.GetAsync($"/api/incidents/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await response.Content.ReadFromJsonAsync<Incident>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Type.Should().Be(created.Type);
    }

    [Fact]
    public async Task GetIncident_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/incidents/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListIncidents_ReturnsPaginatedList()
    {
        await CreateTestIncidentAsync(EmergencyType.Wildfire);
        await CreateTestIncidentAsync(EmergencyType.Flood);

        var response = await _client.GetAsync("/api/incidents?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<IncidentListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListIncidents_FilterByType_ReturnsFiltered()
    {
        await CreateTestIncidentAsync(EmergencyType.Earthquake);

        var response = await _client.GetAsync("/api/incidents?type=Earthquake");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<IncidentListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().AllSatisfy(i => i.Type.Should().Be(EmergencyType.Earthquake));
    }

    [Fact]
    public async Task UpdateStatus_ExistingIncident_UpdatesAndReturns()
    {
        var created = await CreateTestIncidentAsync();

        var updateRequest = new UpdateIncidentStatusRequest(IncidentStatus.InProgress, "Responders en route");
        var response = await _client.PutAsJsonAsync($"/api/incidents/{created.Id}/status", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<Incident>();
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(IncidentStatus.InProgress);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentId_ReturnsNotFound()
    {
        var updateRequest = new UpdateIncidentStatusRequest(IncidentStatus.Resolved);
        var response = await _client.PutAsJsonAsync($"/api/incidents/{Guid.NewGuid()}/status", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_ToResolved_SetsResolvedTimestamp()
    {
        var created = await CreateTestIncidentAsync();

        var updateRequest = new UpdateIncidentStatusRequest(IncidentStatus.Resolved, "All clear");
        var response = await _client.PutAsJsonAsync($"/api/incidents/{created.Id}/status", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolved = await response.Content.ReadFromJsonAsync<Incident>();
        resolved.Should().NotBeNull();
        resolved!.ResolvedAt.Should().NotBeNull();
        resolved.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
