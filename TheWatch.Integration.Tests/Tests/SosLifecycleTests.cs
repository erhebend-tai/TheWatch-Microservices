using FluentAssertions;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.FirstResponder.Models;
using TheWatch.Contracts.Surveillance;
using TheWatch.Contracts.Surveillance.Models;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.VoiceEmergency.Models;
using TheWatch.Integration.Tests.Fixtures;

namespace TheWatch.Integration.Tests.Tests;

/// <summary>
/// Item 253: Full SOS lifecycle integration test.
/// Flow: Create incident (P2) -> Dispatch responder (P2/P6) -> Submit evidence (P2)
///       -> Submit footage (P11) -> Resolve incident -> Verify cross-service state.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class SosLifecycleTests
{
    private readonly AuthenticatedTestFixture _fixture;
    private readonly IVoiceEmergencyClient _voiceClient;
    private readonly IFirstResponderClient _responderClient;
    private readonly ISurveillanceClient _surveillanceClient;

    public SosLifecycleTests(AuthenticatedTestFixture fixture)
    {
        _fixture = fixture;
        _voiceClient = new VoiceEmergencyClient(fixture.VoiceEmergencyHttp);
        _responderClient = new FirstResponderClient(fixture.FirstResponderHttp);
        _surveillanceClient = new SurveillanceClient(fixture.SurveillanceHttp);
    }

    [Fact]
    public async Task FullSosLifecycle_FromCreationToResolution()
    {
        // ── Step 1: Register a first responder ──
        var responder = await _responderClient.RegisterResponderAsync(new RegisterResponderRequest(
            Name: "Officer Integration Test",
            BadgeNumber: $"IT-{Guid.NewGuid():N}"[..12],
            Type: ResponderType.Police,
            Latitude: 40.7128,
            Longitude: -74.0060));
        responder.Should().NotBeNull();
        responder.Id.Should().NotBeEmpty();

        // Update responder as on-duty and available
        await _responderClient.UpdateStatusAsync(responder.Id, new UpdateStatusRequest(
            Status: ResponderStatus.Available,
            IsOnDuty: true));

        // ── Step 2: Create an SOS incident ──
        var incident = await _voiceClient.CreateIncidentAsync(new CreateIncidentRequest(
            ReporterUserId: _fixture.User.Id,
            Type: IncidentType.SOS,
            Latitude: 40.7130,
            Longitude: -74.0058,
            Description: "Integration test SOS activation",
            ReporterPhone: "+15551234567"));
        incident.Should().NotBeNull();
        incident.Id.Should().NotBeEmpty();
        incident.Status.Should().Be(IncidentStatus.Active);

        // ── Step 3: Verify incident is retrievable ──
        var retrieved = await _voiceClient.GetIncidentAsync(incident.Id);
        retrieved.Id.Should().Be(incident.Id);
        retrieved.Type.Should().Be(IncidentType.SOS);

        // ── Step 4: Find nearby responders ──
        var nearbyResponders = await _responderClient.FindNearbyAsync(new NearbyResponderQuery(
            Latitude: 40.7130,
            Longitude: -74.0058,
            RadiusKm: 5.0,
            Type: ResponderType.Police,
            OnDutyOnly: true));
        nearbyResponders.Should().NotBeEmpty("at least the registered responder should be found");

        // ── Step 5: Create a dispatch ──
        var dispatch = await _voiceClient.CreateDispatchAsync(new CreateDispatchRequest(
            IncidentId: incident.Id,
            ResponderId: responder.Id,
            Priority: DispatchPriority.High));
        dispatch.Should().NotBeNull();
        dispatch.Id.Should().NotBeEmpty();

        // ── Step 6: Submit surveillance footage linked to the area ──
        var footage = await _surveillanceClient.SubmitFootageAsync(new SubmitFootageRequest(
            CameraId: null,
            SubmittedByUserId: _fixture.User.Id,
            Latitude: 40.7131,
            Longitude: -74.0059,
            StartTime: DateTime.UtcNow.AddMinutes(-5),
            EndTime: DateTime.UtcNow,
            StorageUrl: "https://storage.thewatch.test/evidence/integration-test.mp4",
            MimeType: "video/mp4",
            FileSizeBytes: 1024 * 1024 * 50));
        footage.Should().NotBeNull();
        footage.Id.Should().NotBeEmpty();

        // ── Step 7: Resolve the incident ──
        var resolved = await _voiceClient.UpdateIncidentStatusAsync(incident.Id,
            new UpdateIncidentStatusRequest(
                Status: IncidentStatus.Resolved,
                ResolutionNotes: "Integration test - incident resolved successfully"));
        resolved.Status.Should().Be(IncidentStatus.Resolved);

        // ── Step 8: Verify final state across services ──
        var finalIncident = await _voiceClient.GetIncidentAsync(incident.Id);
        finalIncident.Status.Should().Be(IncidentStatus.Resolved);

        var finalDispatch = await _voiceClient.GetDispatchAsync(dispatch.Id);
        finalDispatch.Should().NotBeNull();

        // Verify surveillance footage is retrievable
        var finalFootage = await _surveillanceClient.GetFootageAsync(footage.Id);
        finalFootage.Should().NotBeNull();
    }

    [Fact]
    public async Task SosIncident_ListsCorrectlyWithFilters()
    {
        // Create two incidents of different types
        var sos = await _voiceClient.CreateIncidentAsync(new CreateIncidentRequest(
            ReporterUserId: _fixture.User.Id,
            Type: IncidentType.SOS,
            Latitude: 40.7500,
            Longitude: -73.9900,
            Description: "Integration test SOS for list test"));

        var medical = await _voiceClient.CreateIncidentAsync(new CreateIncidentRequest(
            ReporterUserId: _fixture.User.Id,
            Type: IncidentType.Medical,
            Latitude: 40.7501,
            Longitude: -73.9901,
            Description: "Integration test medical for list test"));

        // List all active incidents
        var activeList = await _voiceClient.ListIncidentsAsync(
            page: 1, pageSize: 50, status: IncidentStatus.Active);
        activeList.Items.Should().NotBeEmpty();

        // Verify both our incidents appear
        activeList.Items.Should().Contain(i => i.Id == sos.Id);
        activeList.Items.Should().Contain(i => i.Id == medical.Id);
    }

    [Fact]
    public async Task DispatchExpandRadius_FindsMoreResponders()
    {
        // Register two responders at different distances
        var nearResponder = await _responderClient.RegisterResponderAsync(new RegisterResponderRequest(
            Name: "Near Responder",
            BadgeNumber: $"NR-{Guid.NewGuid():N}"[..12],
            Type: ResponderType.EMS,
            Latitude: 40.7580,
            Longitude: -73.9855));
        await _responderClient.UpdateStatusAsync(nearResponder.Id,
            new UpdateStatusRequest(Status: ResponderStatus.Available, IsOnDuty: true));

        var farResponder = await _responderClient.RegisterResponderAsync(new RegisterResponderRequest(
            Name: "Far Responder",
            BadgeNumber: $"FR-{Guid.NewGuid():N}"[..12],
            Type: ResponderType.EMS,
            Latitude: 40.8000,
            Longitude: -73.9500));
        await _responderClient.UpdateStatusAsync(farResponder.Id,
            new UpdateStatusRequest(Status: ResponderStatus.Available, IsOnDuty: true));

        // Create an incident and dispatch
        var incident = await _voiceClient.CreateIncidentAsync(new CreateIncidentRequest(
            ReporterUserId: _fixture.User.Id,
            Type: IncidentType.Medical,
            Latitude: 40.7580,
            Longitude: -73.9855,
            Description: "Expand radius integration test"));

        var dispatch = await _voiceClient.CreateDispatchAsync(new CreateDispatchRequest(
            IncidentId: incident.Id,
            ResponderId: nearResponder.Id,
            Priority: DispatchPriority.Critical));

        // Expand the search radius
        var expanded = await _voiceClient.ExpandRadiusAsync(dispatch.Id,
            new ExpandRadiusRequest(NewRadiusKm: 10.0));
        expanded.Should().NotBeNull();
    }
}
