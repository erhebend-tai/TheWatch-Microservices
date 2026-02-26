using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P7.FamilyHealth.Family;
using Xunit;

namespace TheWatch.P7.FamilyHealth.Tests;

public class FamilyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FamilyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<FamilyGroup> CreateGroupAsync(string name = "Smith Family")
    {
        var response = await _client.PostAsJsonAsync("/api/families", new CreateFamilyGroupRequest(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<FamilyGroup>())!;
    }

    private async Task<FamilyMember> AddMemberAsync(Guid groupId, string name = "Jane Smith", FamilyRole role = FamilyRole.Parent)
    {
        var response = await _client.PostAsJsonAsync($"/api/families/{groupId}/members",
            new AddMemberRequest(name, role, $"{Guid.NewGuid():N}@test.com", "555-0100"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<FamilyMember>())!;
    }

    [Fact]
    public async Task CreateGroup_ReturnsCreated()
    {
        var group = await CreateGroupAsync("Johnson Family");

        group.Name.Should().Be("Johnson Family");
        group.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddMember_ReturnsCreated()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id, "Billy Smith", FamilyRole.Child);

        member.Name.Should().Be("Billy Smith");
        member.Role.Should().Be(FamilyRole.Child);
        member.FamilyGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task GetGroup_IncludesMembers()
    {
        var group = await CreateGroupAsync();
        await AddMemberAsync(group.Id, "Parent 1", FamilyRole.Parent);
        await AddMemberAsync(group.Id, "Child 1", FamilyRole.Child);

        var response = await _client.GetFromJsonAsync<FamilyGroupResponse>($"/api/families/{group.Id}");
        response!.Group.Id.Should().Be(group.Id);
        response.Members.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetGroup_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/families/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCheckIn_ReturnsCreated()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);
        var request = new CreateCheckInRequest(CheckInStatus.Safe, "All good!", 33.45, -112.07);

        var response = await _client.PostAsJsonAsync($"/api/members/{member.Id}/checkins", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var checkIn = await response.Content.ReadFromJsonAsync<CheckIn>();
        checkIn!.Status.Should().Be(CheckInStatus.Safe);
        checkIn.MemberId.Should().Be(member.Id);
    }

    [Fact]
    public async Task GetCheckIns_ReturnsHistory()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);

        await _client.PostAsJsonAsync($"/api/members/{member.Id}/checkins",
            new CreateCheckInRequest(CheckInStatus.Safe, "Morning check"));
        await _client.PostAsJsonAsync($"/api/members/{member.Id}/checkins",
            new CreateCheckInRequest(CheckInStatus.Safe, "Evening check"));

        var checkIns = await _client.GetFromJsonAsync<List<CheckIn>>($"/api/members/{member.Id}/checkins");
        checkIns!.Count.Should().Be(2);
    }

    [Fact]
    public async Task RecordVital_ReturnsCreated()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);

        var response = await _client.PostAsJsonAsync($"/api/members/{member.Id}/vitals",
            new RecordVitalRequest(VitalType.HeartRate, 72, "bpm"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var reading = await response.Content.ReadFromJsonAsync<VitalReading>();
        reading!.Type.Should().Be(VitalType.HeartRate);
        reading.Value.Should().Be(72);
    }

    [Fact]
    public async Task AbnormalVital_CreatesAlert()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);

        // Record abnormally high heart rate
        await _client.PostAsJsonAsync($"/api/members/{member.Id}/vitals",
            new RecordVitalRequest(VitalType.HeartRate, 150, "bpm"));

        var alerts = await _client.GetFromJsonAsync<List<MedicalAlert>>($"/api/members/{member.Id}/alerts");
        alerts!.Should().HaveCountGreaterThanOrEqualTo(1);
        alerts.First().AlertType.Should().Contain("HeartRate");
    }

    [Fact]
    public async Task AcknowledgeAlert_ReturnsUpdated()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);

        await _client.PostAsJsonAsync($"/api/members/{member.Id}/vitals",
            new RecordVitalRequest(VitalType.SpO2, 88, "%")); // Abnormally low

        var alerts = await _client.GetFromJsonAsync<List<MedicalAlert>>($"/api/members/{member.Id}/alerts");
        var alertId = alerts!.First().Id;

        var response = await _client.PutAsync($"/api/alerts/{alertId}/acknowledge", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var acked = await response.Content.ReadFromJsonAsync<MedicalAlert>();
        acked!.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMember_ReturnsNoContent()
    {
        var group = await CreateGroupAsync();
        var member = await AddMemberAsync(group.Id);

        var response = await _client.DeleteAsync($"/api/members/{member.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
