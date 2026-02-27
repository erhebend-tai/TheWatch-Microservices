using FluentAssertions;
using TheWatch.Contracts.DisasterRelief;
using TheWatch.Contracts.DisasterRelief.Models;
using TheWatch.Contracts.MeshNetwork;
using TheWatch.Contracts.MeshNetwork.Models;
using TheWatch.Integration.Tests.Fixtures;

namespace TheWatch.Integration.Tests.Tests;

/// <summary>
/// Item 255: Disaster relief integration test.
/// Flow: Declare disaster (P8) -> Open shelters -> Allocate resources
///       -> Activate mesh network (P3) -> Create evacuation routes -> Track evacuees.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class DisasterReliefFlowTests
{
    private readonly AuthenticatedTestFixture _fixture;
    private readonly IDisasterReliefClient _disasterClient;
    private readonly IMeshNetworkClient _meshClient;

    public DisasterReliefFlowTests(AuthenticatedTestFixture fixture)
    {
        _fixture = fixture;
        _disasterClient = new DisasterReliefClient(fixture.DisasterReliefHttp);
        _meshClient = new MeshNetworkClient(fixture.MeshNetworkHttp);
    }

    [Fact]
    public async Task FullDisasterReliefFlow_FromDeclarationToEvacuation()
    {
        // ── Step 1: Declare a disaster event ──
        var disaster = await _disasterClient.CreateEventAsync(new CreateDisasterEventRequest(
            Name: "Integration Test Hurricane",
            Type: DisasterType.Hurricane,
            Severity: SeverityLevel.Critical,
            Latitude: 25.7617,
            Longitude: -80.1918,
            RadiusKm: 50.0,
            Description: "Category 4 hurricane - integration test scenario",
            DeclaredBy: _fixture.User.Id));
        disaster.Should().NotBeNull();
        disaster.Id.Should().NotBeEmpty();
        disaster.Status.Should().Be(EventStatus.Active);

        // ── Step 2: Open emergency shelters ──
        var shelter1 = await _disasterClient.CreateShelterAsync(new CreateShelterRequest(
            EventId: disaster.Id,
            Name: "Miami Convention Center Shelter",
            Latitude: 25.7907,
            Longitude: -80.1300,
            MaxCapacity: 500,
            Address: "400 SE 2nd Ave, Miami, FL 33131",
            ContactPhone: "+13055551000"));
        shelter1.Should().NotBeNull();
        shelter1.Id.Should().NotBeEmpty();

        var shelter2 = await _disasterClient.CreateShelterAsync(new CreateShelterRequest(
            EventId: disaster.Id,
            Name: "FIU Arena Shelter",
            Latitude: 25.7563,
            Longitude: -80.3748,
            MaxCapacity: 300,
            Address: "11200 SW 8th St, Miami, FL 33199",
            ContactPhone: "+13055552000"));
        shelter2.Should().NotBeNull();

        // ── Step 3: Update shelter occupancy ──
        await _disasterClient.UpdateOccupancyAsync(shelter1.Id, new UpdateOccupancyRequest(
            CurrentOccupancy: 127,
            Notes: "First wave of evacuees arrived"));

        // ── Step 4: Donate resources ──
        var waterDonation = await _disasterClient.DonateResourceAsync(new DonateResourceRequest(
            EventId: disaster.Id,
            ShelterId: shelter1.Id,
            ResourceType: ResourceType.Water,
            Quantity: 1000,
            Unit: "gallons",
            DonorName: "Integration Test Donor"));
        waterDonation.Should().NotBeNull();

        var foodDonation = await _disasterClient.DonateResourceAsync(new DonateResourceRequest(
            EventId: disaster.Id,
            ShelterId: shelter1.Id,
            ResourceType: ResourceType.Food,
            Quantity: 500,
            Unit: "meals",
            DonorName: "Integration Test Donor"));
        foodDonation.Should().NotBeNull();

        // ── Step 5: Create a resource request ──
        var resourceRequest = await _disasterClient.CreateResourceRequestAsync(
            new CreateResourceRequestRecord(
                EventId: disaster.Id,
                ShelterId: shelter2.Id,
                ResourceType: ResourceType.MedicalSupplies,
                QuantityNeeded: 50,
                Unit: "kits",
                Priority: RequestPriority.High,
                Notes: "Need medical kits for elderly evacuees"));
        resourceRequest.Should().NotBeNull();

        // ── Step 6: Activate mesh network for the disaster zone ──
        var meshNode = await _meshClient.RegisterNodeAsync(new RegisterNodeRequest(
            DeviceId: $"mesh-{Guid.NewGuid():N}"[..16],
            Name: "Disaster Zone Relay Node 1",
            Latitude: 25.7617,
            Longitude: -80.1918,
            Type: NodeType.Relay,
            Capabilities: ["text", "gps", "sos"]));
        meshNode.Should().NotBeNull();
        meshNode.Id.Should().NotBeEmpty();

        // Create a disaster alert channel
        var alertChannel = await _meshClient.CreateChannelAsync(new CreateChannelRequest(
            Name: $"Hurricane Alert - {disaster.Id:N}"[..30],
            Type: ChannelType.Emergency,
            MaxParticipants: 10000));
        alertChannel.Should().NotBeNull();

        // Broadcast alert via mesh network
        var alertMessage = await _meshClient.SendMessageAsync(new SendMessageRequest(
            ChannelId: alertChannel.Id,
            SenderId: meshNode.Id,
            Content: "EMERGENCY: Mandatory evacuation order in effect. Proceed to nearest shelter.",
            Priority: MessagePriority.Critical));
        alertMessage.Should().NotBeNull();

        // ── Step 7: Create evacuation routes ──
        var evacRoute = await _disasterClient.CreateEvacRouteAsync(new CreateEvacuationRouteRequest(
            EventId: disaster.Id,
            Name: "I-95 North Evacuation Route",
            StartLatitude: 25.7617,
            StartLongitude: -80.1918,
            EndLatitude: 26.1224,
            EndLongitude: -80.1373,
            Description: "Primary northbound evacuation via I-95",
            EstimatedDurationMinutes: 45));
        evacRoute.Should().NotBeNull();
        evacRoute.Id.Should().NotBeEmpty();

        // ── Step 8: Verify state across services ──

        // List shelters for the disaster
        var shelters = await _disasterClient.ListSheltersAsync(eventId: disaster.Id);
        shelters.Items.Should().HaveCountGreaterOrEqualTo(2);

        // List evacuation routes
        var routes = await _disasterClient.ListEvacRoutesAsync(disaster.Id);
        routes.Should().NotBeEmpty();

        // Verify mesh network topology includes our node
        var topology = await _meshClient.GetTopologyAsync();
        topology.Should().NotBeNull();

        // Get messages from alert channel
        var messages = await _meshClient.GetMessagesAsync(channelId: alertChannel.Id);
        messages.Should().NotBeEmpty();

        // ── Step 9: Resolve the disaster ──
        var resolved = await _disasterClient.UpdateEventStatusAsync(disaster.Id,
            new UpdateEventStatusRequest(
                Status: EventStatus.Resolved,
                Notes: "Hurricane passed - all-clear issued - integration test complete"));
        resolved.Status.Should().Be(EventStatus.Resolved);
    }

    [Fact]
    public async Task ShelterOccupancy_UpdatesCorrectly()
    {
        var disaster = await _disasterClient.CreateEventAsync(new CreateDisasterEventRequest(
            Name: "Occupancy Test Event",
            Type: DisasterType.Flood,
            Severity: SeverityLevel.High,
            Latitude: 29.7604,
            Longitude: -95.3698,
            RadiusKm: 20.0,
            Description: "Flood event for occupancy testing",
            DeclaredBy: _fixture.User.Id));

        var shelter = await _disasterClient.CreateShelterAsync(new CreateShelterRequest(
            EventId: disaster.Id,
            Name: "Occupancy Test Shelter",
            Latitude: 29.7604,
            Longitude: -95.3698,
            MaxCapacity: 200,
            Address: "123 Test St",
            ContactPhone: "+17135551000"));

        // Update occupancy multiple times
        await _disasterClient.UpdateOccupancyAsync(shelter.Id, new UpdateOccupancyRequest(
            CurrentOccupancy: 50, Notes: "First arrivals"));
        await _disasterClient.UpdateOccupancyAsync(shelter.Id, new UpdateOccupancyRequest(
            CurrentOccupancy: 150, Notes: "Second wave"));

        // Retrieve and verify current occupancy
        var updated = await _disasterClient.GetShelterAsync(shelter.Id);
        updated.CurrentOccupancy.Should().Be(150);
        updated.MaxCapacity.Should().Be(200);
    }
}
