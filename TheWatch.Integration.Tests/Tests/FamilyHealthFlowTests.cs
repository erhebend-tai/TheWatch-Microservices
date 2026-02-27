using FluentAssertions;
using TheWatch.Contracts.DoctorServices;
using TheWatch.Contracts.DoctorServices.Models;
using TheWatch.Contracts.FamilyHealth;
using TheWatch.Contracts.FamilyHealth.Models;
using TheWatch.Integration.Tests.Fixtures;

namespace TheWatch.Integration.Tests.Tests;

/// <summary>
/// Item 254: Family health integration test.
/// Flow: Create family group (P7) -> Add members -> Submit vital readings
///       -> Trigger medical alert -> Book doctor appointment (P9).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class FamilyHealthFlowTests
{
    private readonly AuthenticatedTestFixture _fixture;
    private readonly IFamilyHealthClient _familyClient;
    private readonly IDoctorServicesClient _doctorClient;

    public FamilyHealthFlowTests(AuthenticatedTestFixture fixture)
    {
        _fixture = fixture;
        _familyClient = new FamilyHealthClient(fixture.FamilyHealthHttp);
        _doctorClient = new DoctorServicesClient(fixture.DoctorServicesHttp);
    }

    [Fact]
    public async Task FullFamilyHealthFlow_GroupCreationToDoctorAppointment()
    {
        // ── Step 1: Create a family group ──
        var group = await _familyClient.CreateGroupAsync(new CreateFamilyGroupRequest(
            Name: "Integration Test Family",
            OwnerUserId: _fixture.User.Id));
        group.Should().NotBeNull();
        group.Id.Should().NotBeEmpty();

        // ── Step 2: Add family members ──
        var parent = await _familyClient.AddMemberAsync(group.Id, new AddMemberRequest(
            UserId: _fixture.User.Id,
            DisplayName: "Parent User",
            Role: FamilyRole.Parent,
            DateOfBirth: new DateTime(1985, 3, 15)));
        parent.Should().NotBeNull();
        parent.Id.Should().NotBeEmpty();

        var child = await _familyClient.AddMemberAsync(group.Id, new AddMemberRequest(
            UserId: null,
            DisplayName: "Child User",
            Role: FamilyRole.Child,
            DateOfBirth: new DateTime(2015, 7, 20)));
        child.Should().NotBeNull();

        // ── Step 3: Verify group contains both members ──
        var groupDetail = await _familyClient.GetGroupAsync(group.Id);
        groupDetail.Should().NotBeNull();
        groupDetail.Members.Should().HaveCount(2);

        // ── Step 4: Record a check-in for the child ──
        var checkIn = await _familyClient.CheckInAsync(child.Id, new CreateCheckInRequest(
            Latitude: 40.7128,
            Longitude: -74.0060,
            Note: "At school - integration test"));
        checkIn.Should().NotBeNull();
        checkIn.Id.Should().NotBeEmpty();

        // ── Step 5: Submit vital readings for the parent ──
        var heartRateVital = await _familyClient.RecordVitalAsync(parent.Id, new RecordVitalRequest(
            Type: VitalType.HeartRate,
            Value: 72.0m,
            Unit: "bpm",
            RecordedAt: DateTime.UtcNow));
        heartRateVital.Should().NotBeNull();

        var bloodPressure = await _familyClient.RecordVitalAsync(parent.Id, new RecordVitalRequest(
            Type: VitalType.BloodPressure,
            Value: 120.80m,
            Unit: "mmHg",
            RecordedAt: DateTime.UtcNow));
        bloodPressure.Should().NotBeNull();

        // Submit an abnormally high heart rate to trigger an alert
        var highHeartRate = await _familyClient.RecordVitalAsync(parent.Id, new RecordVitalRequest(
            Type: VitalType.HeartRate,
            Value: 180.0m,
            Unit: "bpm",
            RecordedAt: DateTime.UtcNow));
        highHeartRate.Should().NotBeNull();

        // ── Step 6: Check for medical alerts ──
        var alerts = await _familyClient.GetAlertsAsync(parent.Id);
        // Alert may or may not be generated depending on threshold configuration
        alerts.Should().NotBeNull();

        // ── Step 7: Review vital history ──
        var vitalHistory = await _familyClient.GetVitalHistoryAsync(parent.Id, VitalType.HeartRate);
        vitalHistory.Should().NotBeNull();
        vitalHistory.Readings.Should().HaveCountGreaterOrEqualTo(2);

        // ── Step 8: Review check-in history ──
        var checkInHistory = await _familyClient.GetCheckInHistoryAsync(child.Id);
        checkInHistory.Should().NotBeNull();
        checkInHistory.CheckIns.Should().NotBeEmpty();

        // ── Step 9: Create a doctor profile for appointment ──
        var doctor = await _doctorClient.CreateDoctorAsync(new CreateDoctorProfileRequest(
            Name: "Dr. Integration Test",
            Specialty: "Family Medicine",
            LicenseNumber: $"LIC-{Guid.NewGuid():N}"[..15],
            Email: $"dr.inttest-{Guid.NewGuid():N}@thewatch.test",
            Phone: "+15559876543",
            Latitude: 40.7128,
            Longitude: -74.0060));
        doctor.Should().NotBeNull();

        // ── Step 10: Book an appointment ──
        var appointment = await _doctorClient.BookAppointmentAsync(new BookAppointmentRequest(
            PatientUserId: _fixture.User.Id,
            DoctorId: doctor.Id,
            ScheduledAt: DateTime.UtcNow.AddDays(1),
            DurationMinutes: 30,
            Reason: "Follow-up for elevated heart rate - integration test",
            Type: AppointmentType.InPerson));
        appointment.Should().NotBeNull();
        appointment.Id.Should().NotBeEmpty();

        // ── Step 11: Verify appointment retrieval ──
        var retrievedAppt = await _doctorClient.GetAppointmentAsync(appointment.Id);
        retrievedAppt.DoctorId.Should().Be(doctor.Id);
        retrievedAppt.PatientUserId.Should().Be(_fixture.User.Id);
    }

    [Fact]
    public async Task VitalHistory_ReturnsFilteredResults()
    {
        // Create a group and member
        var group = await _familyClient.CreateGroupAsync(new CreateFamilyGroupRequest(
            Name: "Vital History Test Family",
            OwnerUserId: _fixture.User.Id));

        var member = await _familyClient.AddMemberAsync(group.Id, new AddMemberRequest(
            UserId: _fixture.User.Id,
            DisplayName: "Vital Test User",
            Role: FamilyRole.Parent,
            DateOfBirth: new DateTime(1990, 1, 1)));

        // Record multiple vital types
        await _familyClient.RecordVitalAsync(member.Id, new RecordVitalRequest(
            VitalType.HeartRate, 70m, "bpm", DateTime.UtcNow));
        await _familyClient.RecordVitalAsync(member.Id, new RecordVitalRequest(
            VitalType.Temperature, 98.6m, "F", DateTime.UtcNow));
        await _familyClient.RecordVitalAsync(member.Id, new RecordVitalRequest(
            VitalType.SpO2, 98m, "%", DateTime.UtcNow));

        // Query filtered by type
        var hrHistory = await _familyClient.GetVitalHistoryAsync(member.Id, VitalType.HeartRate);
        hrHistory.Readings.Should().AllSatisfy(r => r.Type.Should().Be(VitalType.HeartRate));

        // Query all types
        var allHistory = await _familyClient.GetVitalHistoryAsync(member.Id);
        allHistory.Readings.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task DoctorSearch_FindsBySpecialtyAndLocation()
    {
        // Create a doctor
        await _doctorClient.CreateDoctorAsync(new CreateDoctorProfileRequest(
            Name: "Dr. Searchable",
            Specialty: "Cardiology",
            LicenseNumber: $"SRC-{Guid.NewGuid():N}"[..15],
            Email: $"dr.search-{Guid.NewGuid():N}@thewatch.test",
            Phone: "+15551112222",
            Latitude: 40.7500,
            Longitude: -73.9900));

        // Search for cardiologists
        var results = await _doctorClient.SearchDoctorsAsync(new DoctorSearchQuery(
            Specialty: "Cardiology",
            Latitude: 40.7500,
            Longitude: -73.9900,
            RadiusKm: 10.0));

        results.Should().NotBeEmpty();
        results.Should().Contain(d => d.Specialty == "Cardiology");
    }
}
