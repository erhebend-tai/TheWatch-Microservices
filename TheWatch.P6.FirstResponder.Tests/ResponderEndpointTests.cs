using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P6.FirstResponder.Responders;
using Xunit;

namespace TheWatch.P6.FirstResponder.Tests;

public class ResponderEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ResponderEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Responder> RegisterResponderAsync(ResponderType type = ResponderType.EMS, string name = "Test Responder")
    {
        var request = new RegisterResponderRequest(name, $"{Guid.NewGuid():N}@test.com", type, Phone: "555-0100");
        var response = await _client.PostAsJsonAsync("/api/responders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Responder>())!;
    }

    [Fact]
    public async Task RegisterResponder_ReturnsCreated()
    {
        var responder = await RegisterResponderAsync(ResponderType.Police, "Officer Smith");

        responder.Name.Should().Be("Officer Smith");
        responder.Type.Should().Be(ResponderType.Police);
        responder.Status.Should().Be(ResponderStatus.OffDuty);
        responder.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetResponder_Existing_ReturnsOk()
    {
        var created = await RegisterResponderAsync();
        var response = await _client.GetAsync($"/api/responders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responder = await response.Content.ReadFromJsonAsync<Responder>();
        responder!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetResponder_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/responders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListResponders_ReturnsPaginated()
    {
        await RegisterResponderAsync(name: "List Test 1");
        await RegisterResponderAsync(name: "List Test 2");

        var response = await _client.GetFromJsonAsync<ResponderListResponse>("/api/responders?page=1&pageSize=50");

        response!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task UpdateLocation_ReturnsUpdatedResponder()
    {
        var responder = await RegisterResponderAsync();
        var locRequest = new UpdateLocationRequest(33.4484, -112.0740, 5.0);

        var response = await _client.PutAsJsonAsync($"/api/responders/{responder.Id}/location", locRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Responder>();
        updated!.LastKnownLocation.Should().NotBeNull();
        updated.LastKnownLocation!.Latitude.Should().Be(33.4484);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUpdatedResponder()
    {
        var responder = await RegisterResponderAsync();
        var statusReq = new UpdateStatusRequest(ResponderStatus.Available);

        var response = await _client.PutAsJsonAsync($"/api/responders/{responder.Id}/status", statusReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Responder>();
        updated!.Status.Should().Be(ResponderStatus.Available);
    }

    [Fact]
    public async Task FindNearby_ReturnsRespondersInRadius()
    {
        // Register and position two responders near Phoenix
        var r1 = await RegisterResponderAsync(name: "Near Responder");
        await _client.PutAsJsonAsync($"/api/responders/{r1.Id}/location", new UpdateLocationRequest(33.4484, -112.0740));
        await _client.PutAsJsonAsync($"/api/responders/{r1.Id}/status", new UpdateStatusRequest(ResponderStatus.Available));

        var r2 = await RegisterResponderAsync(name: "Far Responder");
        await _client.PutAsJsonAsync($"/api/responders/{r2.Id}/location", new UpdateLocationRequest(40.7128, -74.0060)); // NYC
        await _client.PutAsJsonAsync($"/api/responders/{r2.Id}/status", new UpdateStatusRequest(ResponderStatus.Available));

        // Search near Phoenix with 50km radius
        var response = await _client.GetFromJsonAsync<List<ResponderSummary>>(
            "/api/responders/nearby?lat=33.45&lon=-112.07&radiusKm=50");

        response!.Should().Contain(r => r.Id == r1.Id);
        response.Should().NotContain(r => r.Id == r2.Id);
    }

    [Fact]
    public async Task DeactivateResponder_ReturnsNoContent()
    {
        var responder = await RegisterResponderAsync();
        var response = await _client.DeleteAsync($"/api/responders/{responder.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Should no longer appear in listings
        var list = await _client.GetFromJsonAsync<ResponderListResponse>("/api/responders?page=1&pageSize=1000");
        list!.Items.Should().NotContain(r => r.Id == responder.Id);
    }

    [Fact]
    public async Task CreateCheckIn_ReturnsCreated()
    {
        var responder = await RegisterResponderAsync();
        var incidentId = Guid.NewGuid();
        var checkInReq = new CreateCheckInRequest(incidentId, CheckInType.Arrived, 33.4484, -112.0740, "On scene");

        var response = await _client.PostAsJsonAsync($"/api/responders/{responder.Id}/checkins", checkInReq);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var checkIn = await response.Content.ReadFromJsonAsync<CheckIn>();
        checkIn!.IncidentId.Should().Be(incidentId);
        checkIn.Type.Should().Be(CheckInType.Arrived);
    }

    [Fact]
    public async Task CheckIn_Arrived_UpdatesResponderStatus()
    {
        var responder = await RegisterResponderAsync();
        await _client.PutAsJsonAsync($"/api/responders/{responder.Id}/status", new UpdateStatusRequest(ResponderStatus.EnRoute));

        var incidentId = Guid.NewGuid();
        await _client.PostAsJsonAsync($"/api/responders/{responder.Id}/checkins",
            new CreateCheckInRequest(incidentId, CheckInType.Arrived, 33.4484, -112.0740));

        var updated = await _client.GetFromJsonAsync<Responder>($"/api/responders/{responder.Id}");
        updated!.Status.Should().Be(ResponderStatus.OnScene);
    }

    [Fact]
    public async Task GetCheckInsForIncident_ReturnsAll()
    {
        var r1 = await RegisterResponderAsync(name: "R1");
        var r2 = await RegisterResponderAsync(name: "R2");
        var incidentId = Guid.NewGuid();

        await _client.PostAsJsonAsync($"/api/responders/{r1.Id}/checkins",
            new CreateCheckInRequest(incidentId, CheckInType.Arrived, 33.45, -112.07));
        await _client.PostAsJsonAsync($"/api/responders/{r2.Id}/checkins",
            new CreateCheckInRequest(incidentId, CheckInType.Arrived, 33.46, -112.08));

        var checkIns = await _client.GetFromJsonAsync<List<CheckIn>>($"/api/incidents/{incidentId}/checkins");
        checkIns!.Count.Should().Be(2);
    }
}
