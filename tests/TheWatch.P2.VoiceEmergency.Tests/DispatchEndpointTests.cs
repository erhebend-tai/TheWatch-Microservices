using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P2.VoiceEmergency.Emergency;
using Xunit;

namespace TheWatch.P2.VoiceEmergency.Tests;

public class DispatchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DispatchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Incident> CreateTestIncidentAsync()
    {
        var request = new CreateIncidentRequest(
            Type: EmergencyType.ActiveShooter,
            Description: "Dispatch test incident",
            Location: new Location(40.7128, -74.0060),
            ReporterId: Guid.NewGuid(),
            Severity: 5);

        var response = await _client.PostAsJsonAsync("/api/incidents", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Incident>())!;
    }

    [Fact]
    public async Task CreateDispatch_ReturnsCreatedWithDefaults()
    {
        var incident = await CreateTestIncidentAsync();

        var request = new CreateDispatchRequest(incident.Id);
        var response = await _client.PostAsJsonAsync("/api/dispatch", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dispatch = await response.Content.ReadFromJsonAsync<Dispatch>();
        dispatch.Should().NotBeNull();
        dispatch!.Id.Should().NotBeEmpty();
        dispatch.IncidentId.Should().Be(incident.Id);
        dispatch.RadiusKm.Should().Be(5.0);
        dispatch.RespondersRequested.Should().Be(8);
        dispatch.Status.Should().Be(DispatchStatus.Pending);
    }

    [Fact]
    public async Task CreateDispatch_CustomRadius_ReturnsCustomValues()
    {
        var incident = await CreateTestIncidentAsync();

        var request = new CreateDispatchRequest(incident.Id, RadiusKm: 15.0, RespondersRequested: 12);
        var response = await _client.PostAsJsonAsync("/api/dispatch", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dispatch = await response.Content.ReadFromJsonAsync<Dispatch>();
        dispatch!.RadiusKm.Should().Be(15.0);
        dispatch.RespondersRequested.Should().Be(12);
    }

    [Fact]
    public async Task GetDispatch_ExistingId_ReturnsDispatch()
    {
        var incident = await CreateTestIncidentAsync();
        var createResp = await _client.PostAsJsonAsync("/api/dispatch", new CreateDispatchRequest(incident.Id));
        var created = await createResp.Content.ReadFromJsonAsync<Dispatch>();

        var response = await _client.GetAsync($"/api/dispatch/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await response.Content.ReadFromJsonAsync<Dispatch>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetDispatch_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/dispatch/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExpandRadius_IncreasesRadius()
    {
        var incident = await CreateTestIncidentAsync();
        var createResp = await _client.PostAsJsonAsync("/api/dispatch", new CreateDispatchRequest(incident.Id, RadiusKm: 5.0));
        var created = await createResp.Content.ReadFromJsonAsync<Dispatch>();

        var expandRequest = new ExpandRadiusRequest(10.0);
        var response = await _client.PostAsJsonAsync($"/api/dispatch/{created!.Id}/expand", expandRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expanded = await response.Content.ReadFromJsonAsync<Dispatch>();
        expanded!.RadiusKm.Should().Be(15.0); // 5 + 10
    }

    [Fact]
    public async Task ExpandRadius_NonExistentId_ReturnsNotFound()
    {
        var expandRequest = new ExpandRadiusRequest(5.0);
        var response = await _client.PostAsJsonAsync($"/api/dispatch/{Guid.NewGuid()}/expand", expandRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDispatchesForIncident_ReturnsAll()
    {
        var incident = await CreateTestIncidentAsync();

        // Create 2 dispatches for same incident
        await _client.PostAsJsonAsync("/api/dispatch", new CreateDispatchRequest(incident.Id, RadiusKm: 5.0));
        await _client.PostAsJsonAsync("/api/dispatch", new CreateDispatchRequest(incident.Id, RadiusKm: 15.0));

        var response = await _client.GetAsync($"/api/incidents/{incident.Id}/dispatches");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dispatches = await response.Content.ReadFromJsonAsync<List<Dispatch>>();
        dispatches.Should().NotBeNull();
        dispatches!.Count.Should().BeGreaterThanOrEqualTo(2);
        dispatches.Should().AllSatisfy(d => d.IncidentId.Should().Be(incident.Id));
    }
}
