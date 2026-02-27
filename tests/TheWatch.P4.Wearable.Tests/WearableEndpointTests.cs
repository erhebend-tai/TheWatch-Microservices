using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P4.Wearable.Devices;
using Xunit;

namespace TheWatch.P4.Wearable.Tests;

public class WearableEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WearableEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<WearableDevice> RegisterDeviceAsync(string name = "My Watch", DevicePlatform platform = DevicePlatform.AppleWatch)
    {
        var response = await _client.PostAsJsonAsync("/api/devices",
            new RegisterDeviceRequest(name, platform, Guid.NewGuid(), "Series 9", "10.2.1"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<WearableDevice>())!;
    }

    [Fact]
    public async Task RegisterDevice_ReturnsCreated()
    {
        var device = await RegisterDeviceAsync("Garmin Fenix", DevicePlatform.Garmin);
        device.Name.Should().Be("Garmin Fenix");
        device.Platform.Should().Be(DevicePlatform.Garmin);
    }

    [Fact]
    public async Task GetDevice_ReturnsOk()
    {
        var created = await RegisterDeviceAsync();
        var response = await _client.GetAsync($"/api/devices/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateDeviceStatus_ReturnsUpdated()
    {
        var device = await RegisterDeviceAsync();
        var response = await _client.PutAsJsonAsync($"/api/devices/{device.Id}/status",
            new UpdateDeviceStatusRequest(DeviceStatus.Connected, 85));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WearableDevice>();
        updated!.Status.Should().Be(DeviceStatus.Connected);
        updated.BatteryPercent.Should().Be(85);
    }

    [Fact]
    public async Task RecordHeartbeat_ReturnsCreated()
    {
        var device = await RegisterDeviceAsync();
        var response = await _client.PostAsJsonAsync($"/api/devices/{device.Id}/heartbeats",
            new RecordHeartbeatRequest(72, 5400, 280));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var reading = await response.Content.ReadFromJsonAsync<HeartbeatReading>();
        reading!.Bpm.Should().Be(72);
        reading.StepCount.Should().Be(5400);
    }

    [Fact]
    public async Task GetHeartbeatHistory_ReturnsReadings()
    {
        var device = await RegisterDeviceAsync();
        await _client.PostAsJsonAsync($"/api/devices/{device.Id}/heartbeats", new RecordHeartbeatRequest(70));
        await _client.PostAsJsonAsync($"/api/devices/{device.Id}/heartbeats", new RecordHeartbeatRequest(75));

        var history = await _client.GetFromJsonAsync<HeartbeatHistory>($"/api/devices/{device.Id}/heartbeats");
        history!.Readings.Count.Should().Be(2);
    }

    [Fact]
    public async Task StartSync_ReturnsCreated()
    {
        var device = await RegisterDeviceAsync();
        var response = await _client.PostAsJsonAsync($"/api/devices/{device.Id}/sync",
            new StartSyncRequest(SyncDirection.DeviceToServer));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var job = await response.Content.ReadFromJsonAsync<SyncJob>();
        job!.Success.Should().BeTrue();
        job.DeviceId.Should().Be(device.Id);
    }
}
