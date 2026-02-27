using FluentAssertions;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.FirstResponder.Models;
using TheWatch.Contracts.MeshNetwork;
using TheWatch.Contracts.MeshNetwork.Models;
using TheWatch.Contracts.Surveillance;
using TheWatch.Contracts.Surveillance.Models;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.VoiceEmergency.Models;
using TheWatch.Integration.Tests.Fixtures;

namespace TheWatch.Integration.Tests.Tests;

/// <summary>
/// Item 220: Full incident lifecycle across P2 -> P6 -> P3 -> P11 via typed clients.
/// Verifies that typed clients correctly relay data across the entire service mesh.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class CrossServiceIncidentTests
{
    private readonly AuthenticatedTestFixture _fixture;
    private readonly IVoiceEmergencyClient _voiceClient;
    private readonly IFirstResponderClient _responderClient;
    private readonly IMeshNetworkClient _meshClient;
    private readonly ISurveillanceClient _surveillanceClient;

    public CrossServiceIncidentTests(AuthenticatedTestFixture fixture)
    {
        _fixture = fixture;
        _voiceClient = new VoiceEmergencyClient(fixture.VoiceEmergencyHttp);
        _responderClient = new FirstResponderClient(fixture.FirstResponderHttp);
        _meshClient = new MeshNetworkClient(fixture.MeshNetworkHttp);
        _surveillanceClient = new SurveillanceClient(fixture.SurveillanceHttp);
    }

    [Fact]
    public async Task IncidentLifecycle_P2_P6_P3_P11_FullRoundTrip()
    {
        // ── P6: Register and activate a responder ──
        var responder = await _responderClient.RegisterResponderAsync(new RegisterResponderRequest(
            Name: "Cross-Service Officer",
            BadgeNumber: $"XS-{Guid.NewGuid():N}"[..12],
            Type: ResponderType.Police,
            Latitude: 34.0522,
            Longitude: -118.2437));
        await _responderClient.UpdateStatusAsync(responder.Id,
            new UpdateStatusRequest(Status: ResponderStatus.Available, IsOnDuty: true));

        // ── P2: Create an incident ──
        var incident = await _voiceClient.CreateIncidentAsync(new CreateIncidentRequest(
            ReporterUserId: _fixture.User.Id,
            Type: IncidentType.Crime,
            Latitude: 34.0525,
            Longitude: -118.2440,
            Description: "Cross-service integration test incident",
            ReporterPhone: "+12135551234"));
        incident.Id.Should().NotBeEmpty();

        // ── P6: Find nearby responders (query from FirstResponder service) ──
        var nearby = await _responderClient.FindNearbyAsync(new NearbyResponderQuery(
            Latitude: 34.0525,
            Longitude: -118.2440,
            RadiusKm: 2.0,
            Type: null,
            OnDutyOnly: true));
        nearby.Should().NotBeEmpty();

        // ── P2: Create a dispatch linking incident to responder ──
        var dispatch = await _voiceClient.CreateDispatchAsync(new CreateDispatchRequest(
            IncidentId: incident.Id,
            ResponderId: responder.Id,
            Priority: DispatchPriority.High));
        dispatch.Id.Should().NotBeEmpty();

        // ── P6: Responder creates a check-in at the scene ──
        var checkIn = await _responderClient.CreateCheckInAsync(responder.Id,
            new CreateCheckInRequest(
                Latitude: 34.0525,
                Longitude: -118.2440,
                IncidentId: incident.Id,
                Note: "On scene - cross-service integration test"));
        checkIn.Should().NotBeNull();

        // ── P3: Broadcast mesh alert ──
        var channel = await _meshClient.CreateChannelAsync(new CreateChannelRequest(
            Name: $"Incident-{incident.Id:N}"[..30],
            Type: ChannelType.Emergency,
            MaxParticipants: 100));

        var meshNode = await _meshClient.RegisterNodeAsync(new RegisterNodeRequest(
            DeviceId: $"dev-{Guid.NewGuid():N}"[..16],
            Name: "Scene Mesh Node",
            Latitude: 34.0525,
            Longitude: -118.2440,
            Type: NodeType.Mobile,
            Capabilities: ["text", "gps"]));

        var meshAlert = await _meshClient.SendMessageAsync(new SendMessageRequest(
            ChannelId: channel.Id,
            SenderId: meshNode.Id,
            Content: $"ALERT: Active incident at scene. Incident ID: {incident.Id}",
            Priority: MessagePriority.High));
        meshAlert.Id.Should().NotBeEmpty();

        // ── P11: Submit surveillance footage near the incident ──
        var footage = await _surveillanceClient.SubmitFootageAsync(new SubmitFootageRequest(
            CameraId: null,
            SubmittedByUserId: _fixture.User.Id,
            Latitude: 34.0526,
            Longitude: -118.2438,
            StartTime: DateTime.UtcNow.AddMinutes(-10),
            EndTime: DateTime.UtcNow,
            StorageUrl: "https://storage.thewatch.test/footage/cross-service-test.mp4",
            MimeType: "video/mp4",
            FileSizeBytes: 1024 * 1024 * 25));
        footage.Id.Should().NotBeEmpty();

        // ── P11: Report a crime location ──
        var crimeLocation = await _surveillanceClient.ReportCrimeLocationAsync(
            new ReportCrimeLocationRequest(
                Latitude: 34.0525,
                Longitude: -118.2440,
                Description: "Crime scene - cross-service test",
                ReportedByUserId: _fixture.User.Id,
                IncidentId: incident.Id));
        crimeLocation.Id.Should().NotBeEmpty();

        // ── P11: Get footage near the crime location ──
        var nearbyFootage = await _surveillanceClient.GetFootageNearCrimeLocationAsync(
            crimeLocation.Id, radiusKm: 1.0);
        nearbyFootage.Should().NotBeEmpty();

        // ── P2: Resolve the incident ──
        var resolved = await _voiceClient.UpdateIncidentStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest(
                Status: IncidentStatus.Resolved,
                ResolutionNotes: "Cross-service integration test complete"));
        resolved.Status.Should().Be(IncidentStatus.Resolved);

        // ── Verify mesh messages persisted ──
        var channelMessages = await _meshClient.GetMessagesAsync(channelId: channel.Id);
        channelMessages.Should().NotBeEmpty();

        // ── Verify surveillance stats include our data ──
        var stats = await _surveillanceClient.GetStatsAsync();
        stats.Should().NotBeNull();
    }
}
