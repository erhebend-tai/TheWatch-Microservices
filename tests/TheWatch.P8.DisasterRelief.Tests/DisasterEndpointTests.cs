using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P8.DisasterRelief.Relief;
using Xunit;

namespace TheWatch.P8.DisasterRelief.Tests;

public class DisasterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DisasterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<DisasterEvent> CreateEventAsync(DisasterType type = DisasterType.Wildfire, string name = "Test Event")
    {
        var request = new CreateDisasterEventRequest(type, name, "Test description", 33.4484, -112.0740, 15.0, 4);
        var response = await _client.PostAsJsonAsync("/api/events", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<DisasterEvent>())!;
    }

    [Fact]
    public async Task CreateEvent_ReturnsCreated()
    {
        var evt = await CreateEventAsync(DisasterType.Hurricane, "Hurricane Alpha");

        evt.Name.Should().Be("Hurricane Alpha");
        evt.Type.Should().Be(DisasterType.Hurricane);
        evt.Status.Should().Be(EventStatus.Active);
        evt.Id.Should().NotBeEmpty();
        evt.Severity.Should().Be(4);
    }

    [Fact]
    public async Task GetEvent_Existing_ReturnsOk()
    {
        var created = await CreateEventAsync();
        var response = await _client.GetAsync($"/api/events/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var evt = await response.Content.ReadFromJsonAsync<DisasterEvent>();
        evt!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetEvent_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListEvents_ReturnsPaginated()
    {
        await CreateEventAsync(name: "Event A");
        await CreateEventAsync(name: "Event B");

        var response = await _client.GetFromJsonAsync<DisasterEventListResponse>("/api/events?page=1&pageSize=50");
        response!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateEventStatus_ReturnsUpdated()
    {
        var evt = await CreateEventAsync();
        var response = await _client.PutAsJsonAsync($"/api/events/{evt.Id}/status",
            new UpdateEventStatusRequest(EventStatus.Resolved));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<DisasterEvent>();
        updated!.Status.Should().Be(EventStatus.Resolved);
    }

    [Fact]
    public async Task CreateShelter_ReturnsCreated()
    {
        var request = new CreateShelterRequest("Phoenix Shelter", 33.45, -112.07, 200, "555-0100", ["Water", "Beds"]);
        var response = await _client.PostAsJsonAsync("/api/shelters", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shelter = await response.Content.ReadFromJsonAsync<Shelter>();
        shelter!.Name.Should().Be("Phoenix Shelter");
        shelter.Capacity.Should().Be(200);
    }

    [Fact]
    public async Task FindNearbyShelters_ReturnsInRadius()
    {
        // Create shelter in Phoenix
        await _client.PostAsJsonAsync("/api/shelters",
            new CreateShelterRequest("Nearby Shelter", 33.4484, -112.0740, 100));
        // Create shelter in NYC (far away)
        await _client.PostAsJsonAsync("/api/shelters",
            new CreateShelterRequest("Far Shelter", 40.7128, -74.0060, 100));

        var results = await _client.GetFromJsonAsync<List<ShelterSummary>>(
            "/api/shelters/nearby?lat=33.45&lon=-112.07&radiusKm=50");

        results!.Should().Contain(s => s.Name == "Nearby Shelter");
        results.Should().NotContain(s => s.Name == "Far Shelter");
    }

    [Fact]
    public async Task UpdateOccupancy_AutoSetsFull()
    {
        var createResp = await _client.PostAsJsonAsync("/api/shelters",
            new CreateShelterRequest("Small Shelter", 33.45, -112.07, 10));
        var shelter = await createResp.Content.ReadFromJsonAsync<Shelter>();

        var response = await _client.PutAsJsonAsync($"/api/shelters/{shelter!.Id}/occupancy",
            new UpdateOccupancyRequest(10));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Shelter>();
        updated!.Status.Should().Be(ShelterStatus.Full);
    }

    [Fact]
    public async Task DonateResource_ReturnsCreated()
    {
        var request = new DonateResourceRequest(ResourceCategory.Water, "Water Bottles", 500, "bottles", 33.45, -112.07);
        var response = await _client.PostAsJsonAsync("/api/resources/donate", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<ResourceItem>();
        item!.Category.Should().Be(ResourceCategory.Water);
        item.Quantity.Should().Be(500);
    }

    [Fact]
    public async Task RequestResource_ReturnsCreated()
    {
        var request = new CreateResourceRequestRecord(Guid.NewGuid(), ResourceCategory.Medical, 50, RequestPriority.Critical, 33.45, -112.07);
        var response = await _client.PostAsJsonAsync("/api/resources/request", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var req = await response.Content.ReadFromJsonAsync<ResourceRequest>();
        req!.Priority.Should().Be(RequestPriority.Critical);
        req.Status.Should().Be(RequestStatus.Open);
    }

    [Fact]
    public async Task ListResources_FiltersByCategory()
    {
        await _client.PostAsJsonAsync("/api/resources/donate",
            new DonateResourceRequest(ResourceCategory.Food, "MREs", 200, "meals", 33.45, -112.07));
        await _client.PostAsJsonAsync("/api/resources/donate",
            new DonateResourceRequest(ResourceCategory.Water, "Gallons", 100, "gallons", 33.45, -112.07));

        var foodOnly = await _client.GetFromJsonAsync<List<ResourceItem>>("/api/resources?category=Food");
        foodOnly!.Should().OnlyContain(r => r.Category == ResourceCategory.Food);
    }

    [Fact]
    public async Task AddEvacuationRoute_ReturnsCreated()
    {
        var evt = await CreateEventAsync();
        var routeReq = new CreateEvacuationRouteRequest(evt.Id, 33.45, -112.07, 33.50, -111.90, 18.5, 25, "I-10 Eastbound");
        var response = await _client.PostAsJsonAsync($"/api/events/{evt.Id}/routes", routeReq);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = await response.Content.ReadFromJsonAsync<EvacuationRoute>();
        route!.DisasterEventId.Should().Be(evt.Id);
        route.DistanceKm.Should().Be(18.5);
    }
}
