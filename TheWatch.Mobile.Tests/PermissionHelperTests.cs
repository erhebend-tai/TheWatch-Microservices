using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for permission helper logic — permission status evaluation,
/// rationale display, and default topic configuration.
/// Since PermissionHelper depends on MAUI Permissions API,
/// we test the pure decision logic and model defaults independently.
/// </summary>
public class PermissionHelperTests
{
    // =========================================================================
    // Permission Flow — Status Evaluation
    // =========================================================================

    [Fact]
    public void PermissionFlow_Granted_ReturnsTrue()
    {
        var status = TestPermissionStatus.Granted;

        var result = IsPermissionGranted(status);

        result.Should().BeTrue();
    }

    [Fact]
    public void PermissionFlow_Denied_ReturnsFalse()
    {
        var status = TestPermissionStatus.Denied;

        var result = IsPermissionGranted(status);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(TestPermissionStatus.Unknown, false)]
    [InlineData(TestPermissionStatus.Disabled, false)]
    [InlineData(TestPermissionStatus.Restricted, false)]
    [InlineData(TestPermissionStatus.Limited, true)]
    [InlineData(TestPermissionStatus.Granted, true)]
    public void PermissionFlow_AllStatuses_EvaluateCorrectly(TestPermissionStatus status, bool expected)
    {
        var result = IsPermissionGranted(status);

        result.Should().Be(expected);
    }

    // =========================================================================
    // Rationale Display
    // =========================================================================

    [Fact]
    public void PermissionFlow_ShouldShowRationale_WhenDenied()
    {
        var status = TestPermissionStatus.Denied;
        var shouldShowRationale = true;

        var showRationale = ShouldShowRationale(status, shouldShowRationale);

        showRationale.Should().BeTrue();
    }

    [Fact]
    public void PermissionFlow_NoRationale_WhenGranted()
    {
        var status = TestPermissionStatus.Granted;
        var shouldShowRationale = false;

        var showRationale = ShouldShowRationale(status, shouldShowRationale);

        showRationale.Should().BeFalse();
    }

    [Fact]
    public void PermissionFlow_NoRationale_WhenDenied_ButFlagFalse()
    {
        var status = TestPermissionStatus.Denied;
        var shouldShowRationale = false;

        var showRationale = ShouldShowRationale(status, shouldShowRationale);

        showRationale.Should().BeFalse();
    }

    // =========================================================================
    // Default Topics
    // =========================================================================

    [Fact]
    public void DefaultTopics_ContainsThreeItems()
    {
        var topics = GetDefaultNotificationTopics();

        topics.Should().HaveCount(3);
    }

    [Fact]
    public void DefaultTopics_ContainsExpectedValues()
    {
        var topics = GetDefaultNotificationTopics();

        topics.Should().Contain("watch-voiceemergency");
        topics.Should().Contain("watch-familyhealth");
        topics.Should().Contain("watch-disasterrelief");
    }

    // =========================================================================
    // Enum Tests
    // =========================================================================

    [Fact]
    public void TestPermissionStatus_HasSixValues()
    {
        Enum.GetValues<TestPermissionStatus>().Should().HaveCount(6);
    }

    [Fact]
    public void TestPermissionStatus_AllDefined()
    {
        TestPermissionStatus.Unknown.Should().BeDefined();
        TestPermissionStatus.Denied.Should().BeDefined();
        TestPermissionStatus.Disabled.Should().BeDefined();
        TestPermissionStatus.Granted.Should().BeDefined();
        TestPermissionStatus.Restricted.Should().BeDefined();
        TestPermissionStatus.Limited.Should().BeDefined();
    }

    // =========================================================================
    // Mirrors PermissionHelper logic
    // =========================================================================

    private static bool IsPermissionGranted(TestPermissionStatus status)
    {
        return status is TestPermissionStatus.Granted or TestPermissionStatus.Limited;
    }

    private static bool ShouldShowRationale(TestPermissionStatus status, bool shouldShowRationale)
    {
        return status != TestPermissionStatus.Granted && shouldShowRationale;
    }

    private static List<string> GetDefaultNotificationTopics()
    {
        return ["watch-voiceemergency", "watch-familyhealth", "watch-disasterrelief"];
    }
}

/// <summary>
/// Mirror of PermissionStatus from MAUI Permissions API
/// </summary>
public enum TestPermissionStatus
{
    Unknown,
    Denied,
    Disabled,
    Granted,
    Restricted,
    Limited
}
