using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Tests;

public class SurveillanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SurveillanceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Tests run without auth
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Info_ReturnsServiceInfo()
    {
        var response = await _client.GetAsync("/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("P11.Surveillance");
    }

    [Fact]
    public async Task RegisterCamera_ReturnsCreated()
    {
        var request = new RegisterCameraRequest(
            OwnerId: Guid.NewGuid(),
            Latitude: 34.0522,
            Longitude: -118.2437,
            Address: "123 Test St, Los Angeles, CA",
            CoverageRadiusMeters: 100,
            CameraModel: "Ring Doorbell Pro",
            IsPublic: true,
            Description: "Front door camera"
        );

        var response = await _client.PostAsJsonAsync("/api/cameras", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var camera = await response.Content.ReadFromJsonAsync<CameraRegistration>();
        camera.Should().NotBeNull();
        camera!.Status.Should().Be(CameraStatus.Pending);
        camera.Latitude.Should().Be(34.0522);
    }

    [Fact]
    public async Task ListCameras_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/cameras?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCamera_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/cameras/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitFootage_ReturnsCreated()
    {
        // First register a camera
        var cameraReq = new RegisterCameraRequest(
            OwnerId: Guid.NewGuid(),
            Latitude: 34.0522,
            Longitude: -118.2437
        );
        var cameraResp = await _client.PostAsJsonAsync("/api/cameras", cameraReq);
        var camera = await cameraResp.Content.ReadFromJsonAsync<CameraRegistration>();

        var request = new SubmitFootageRequest(
            CameraId: camera!.Id,
            SubmitterId: Guid.NewGuid(),
            GpsLatitude: 34.0522,
            GpsLongitude: -118.2437,
            StartTime: DateTime.UtcNow.AddHours(-1),
            EndTime: DateTime.UtcNow,
            MediaUrl: "https://storage.example.com/footage/test.mp4",
            MediaType: MediaType.Video,
            Description: "Test footage submission"
        );

        var response = await _client.PostAsJsonAsync("/api/footage", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SubmitFootage_WithAudioType_ReturnsCreated()
    {
        var cameraReq = new RegisterCameraRequest(
            OwnerId: Guid.NewGuid(),
            Latitude: 40.7128,
            Longitude: -74.0060
        );
        var cameraResp = await _client.PostAsJsonAsync("/api/cameras", cameraReq);
        var camera = await cameraResp.Content.ReadFromJsonAsync<CameraRegistration>();

        var request = new SubmitFootageRequest(
            CameraId: camera!.Id,
            SubmitterId: Guid.NewGuid(),
            GpsLatitude: 40.7128,
            GpsLongitude: -74.0060,
            StartTime: DateTime.UtcNow.AddHours(-1),
            EndTime: DateTime.UtcNow,
            MediaUrl: "https://storage.example.com/audio/recording.wav",
            MediaType: MediaType.Audio,
            Description: "Audio recording from scene"
        );

        var response = await _client.PostAsJsonAsync("/api/footage", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SubmitFootage_WithFileHash_ReturnsCreated()
    {
        var cameraReq = new RegisterCameraRequest(
            OwnerId: Guid.NewGuid(),
            Latitude: 34.0522,
            Longitude: -118.2437
        );
        var cameraResp = await _client.PostAsJsonAsync("/api/cameras", cameraReq);
        var camera = await cameraResp.Content.ReadFromJsonAsync<CameraRegistration>();

        var request = new SubmitFootageRequest(
            CameraId: camera!.Id,
            SubmitterId: Guid.NewGuid(),
            GpsLatitude: 34.0522,
            GpsLongitude: -118.2437,
            StartTime: DateTime.UtcNow.AddHours(-1),
            EndTime: DateTime.UtcNow,
            MediaUrl: "https://storage.example.com/footage/evidence.mp4",
            MediaType: MediaType.Video,
            FileHashSha256: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            Description: "Footage with integrity hash"
        );

        var response = await _client.PostAsJsonAsync("/api/footage", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ListFootage_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/footage?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReportCrimeLocation_ReturnsCreated()
    {
        var request = new ReportCrimeLocationRequest(
            Latitude: 34.0522,
            Longitude: -118.2437,
            Description: "Reported break-in at warehouse",
            ReporterId: Guid.NewGuid(),
            CrimeType: "Burglary",
            OccurredAt: DateTime.UtcNow.AddHours(-3)
        );

        var response = await _client.PostAsJsonAsync("/api/crime-locations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ListCrimeLocations_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/crime-locations?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSurveillanceStats_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/surveillance/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
