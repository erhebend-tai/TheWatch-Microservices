using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P6.FirstResponder.Responders;
using Xunit;

namespace TheWatch.P6.FirstResponder.Tests;

public class DesignatedResponderEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DesignatedResponderEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<DesignatedResponder> SignupAsync(string name = "Test Volunteer", double lat = 33.4484, double lon = -112.0740)
    {
        var request = new SignupDesignatedResponderRequest(
            name,
            $"{Guid.NewGuid():N}@test.com",
            lat, lon,
            ResponseRadiusKm: 5.0,
            Phone: null,
            LocationDescription: "Test Location",
            Schedules:
            [
                new ScheduleEntry(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new ScheduleEntry(DayOfWeek.Wednesday, new TimeOnly(10, 0), new TimeOnly(14, 0))
            ],
            Skills: ["First Aid", "CPR"],
            Notes: "Available for emergencies");

        var response = await _client.PostAsJsonAsync("/api/designated-responders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<DesignatedResponder>())!;
    }

    [Fact]
    public async Task Signup_ReturnsCreated()
    {
        var responder = await SignupAsync("Jane Doe");

        responder.VolunteerName.Should().Be("Jane Doe");
        responder.Status.Should().Be(DesignatedResponderStatus.Pending);
        responder.ResponseRadiusKm.Should().Be(5.0);
        responder.Skills.Should().Contain("First Aid");
        responder.Schedules.Should().HaveCount(2);
        responder.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var created = await SignupAsync();
        var response = await _client.GetAsync($"/api/designated-responders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responder = await response.Content.ReadFromJsonAsync<DesignatedResponder>();
        responder!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/designated-responders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ReturnsPaginated()
    {
        await SignupAsync(name: "List Test 1");
        await SignupAsync(name: "List Test 2");

        var response = await _client.GetFromJsonAsync<DesignatedResponderListResponse>(
            "/api/designated-responders?page=1&pageSize=50");

        response!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsUpdated()
    {
        var created = await SignupAsync();
        var statusReq = new UpdateDesignatedResponderStatusRequest(DesignatedResponderStatus.Approved);

        var response = await _client.PutAsJsonAsync(
            $"/api/designated-responders/{created.Id}/status", statusReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<DesignatedResponder>();
        updated!.Status.Should().Be(DesignatedResponderStatus.Approved);
    }

    [Fact]
    public async Task MapEndpoint_ReturnsMapItems()
    {
        var created = await SignupAsync(name: "Map Test", lat: 34.0522, lon: -118.2437);

        var items = await _client.GetFromJsonAsync<List<DesignatedResponderMapItem>>(
            "/api/designated-responders/map");

        items.Should().NotBeNull();
        items!.Should().Contain(i => i.Id == created.Id);
        var item = items.First(i => i.Id == created.Id);
        item.VolunteerName.Should().Be("Map Test");
        item.Latitude.Should().Be(34.0522);
        item.Longitude.Should().Be(-118.2437);
        item.Schedules.Should().HaveCount(2);
    }

    [Fact]
    public async Task MapEndpoint_FiltersByStatus()
    {
        var created = await SignupAsync(name: "Filter Test");

        // Default status is Pending, filter for Active should not include it
        var activeItems = await _client.GetFromJsonAsync<List<DesignatedResponderMapItem>>(
            "/api/designated-responders/map?status=Active");

        activeItems!.Should().NotContain(i => i.Id == created.Id);

        // Filter for Pending should include it
        var pendingItems = await _client.GetFromJsonAsync<List<DesignatedResponderMapItem>>(
            "/api/designated-responders/map?status=Pending");

        pendingItems!.Should().Contain(i => i.Id == created.Id);
    }

    [Fact]
    public async Task Signup_IncludesSchedules()
    {
        var responder = await SignupAsync();

        responder.Schedules.Should().HaveCount(2);
        responder.Schedules.Should().Contain(s => s.DayOfWeek == DayOfWeek.Monday);
        responder.Schedules.Should().Contain(s => s.DayOfWeek == DayOfWeek.Wednesday);
    }
}
