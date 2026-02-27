using FluentAssertions;
using TheWatch.Shared.Contracts.Mobile;
using Xunit;

namespace TheWatch.Mobile.Tests;

public class SharedContractsTests
{
    [Fact]
    public void LoginRequest_CanBeCreated()
    {
        var req = new LoginRequest("test@example.com", "password123");
        req.Email.Should().Be("test@example.com");
        req.Password.Should().Be("password123");
    }

    [Fact]
    public void RegisterRequest_CanBeCreatedWithOptionalPhone()
    {
        var req = new RegisterRequest("test@example.com", "pass", "Test User");
        req.Phone.Should().BeNull();

        var reqWithPhone = new RegisterRequest("test@example.com", "pass", "Test User", "555-1234");
        reqWithPhone.Phone.Should().Be("555-1234");
    }

    [Fact]
    public void LoginResponse_ContainsAllFields()
    {
        var user = new UserInfoDto(Guid.NewGuid(), "test@example.com", "Test", null, ["user"], DateTime.UtcNow);
        var resp = new LoginResponse("at", "rt", DateTime.UtcNow.AddHours(1), user);
        resp.AccessToken.Should().Be("at");
        resp.RefreshToken.Should().Be("rt");
        resp.User.Should().NotBeNull();
    }

    [Fact]
    public void CreateIncidentRequest_HasDefaults()
    {
        var loc = new LocationDto(40.7128, -74.0060);
        var req = new CreateIncidentRequest(EmergencyType.MedicalEmergency, "Test", loc, Guid.NewGuid());
        req.Severity.Should().Be(3);
        req.Tags.Should().BeNull();
    }

    [Fact]
    public void EmergencyType_HasExpectedValues()
    {
        Enum.GetValues<EmergencyType>().Should().HaveCount(10);
        EmergencyType.Wildfire.Should().BeDefined();
        EmergencyType.ActiveShooter.Should().BeDefined();
        EmergencyType.Other.Should().BeDefined();
    }

    [Fact]
    public void FamilyGroupDto_CanBeCreated()
    {
        var members = new List<FamilyMemberDto>
        {
            new(Guid.NewGuid(), "Alice", "alice@test.com", null, FamilyRole.Parent, Guid.NewGuid()),
            new(Guid.NewGuid(), "Bob", null, null, FamilyRole.Child, Guid.NewGuid()),
        };
        var group = new FamilyGroupDto(Guid.NewGuid(), "Test Family", members);
        group.Members.Should().HaveCount(2);
    }

    [Fact]
    public void VitalReadingDto_SupportsAllTypes()
    {
        var types = Enum.GetValues<VitalType>();
        types.Should().HaveCount(6);
        types.Should().Contain(VitalType.HeartRate);
        types.Should().Contain(VitalType.SpO2);
    }

    [Fact]
    public void CheckInStatus_HasExpectedValues()
    {
        Enum.GetValues<CheckInStatus>().Should().HaveCount(4);
        CheckInStatus.Safe.Should().BeDefined();
        CheckInStatus.Emergency.Should().BeDefined();
    }

    [Fact]
    public void LocationDto_SupportsOptionalFields()
    {
        var loc = new LocationDto(37.7749, -122.4194);
        loc.Accuracy.Should().BeNull();
        loc.Timestamp.Should().BeNull();

        var locFull = new LocationDto(37.7749, -122.4194, 10.5, DateTime.UtcNow);
        locFull.Accuracy.Should().Be(10.5);
    }

    [Fact]
    public void IncidentListResponse_CanBeCreated()
    {
        var items = new List<IncidentDto>();
        var resp = new IncidentListResponse(items, 0, 1, 10);
        resp.TotalCount.Should().Be(0);
        resp.PageSize.Should().Be(10);
    }
}
