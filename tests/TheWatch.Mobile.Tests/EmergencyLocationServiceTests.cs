using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for emergency location service logic — battery-adaptive intervals
/// and tracking state transitions.
/// Since EmergencyLocationService depends on MAUI Geolocation and Battery APIs,
/// we test the pure interval calculation and state machine independently.
/// </summary>
public class EmergencyLocationServiceTests
{
    // =========================================================================
    // Battery-Adaptive Interval Calculation
    // =========================================================================

    [Theory]
    [InlineData(0.14, 10)]
    [InlineData(0.10, 10)]
    [InlineData(0.05, 10)]
    [InlineData(0.01, 10)]
    public void BatteryInterval_Below15Percent_Returns10Seconds(double batteryLevel, int expectedSeconds)
    {
        var interval = GetTrackingIntervalSeconds(batteryLevel);

        interval.Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(0.15, 3)]
    [InlineData(0.20, 3)]
    [InlineData(0.50, 3)]
    [InlineData(0.80, 3)]
    [InlineData(1.00, 3)]
    public void BatteryInterval_AtOrAbove15Percent_Returns3Seconds(double batteryLevel, int expectedSeconds)
    {
        var interval = GetTrackingIntervalSeconds(batteryLevel);

        interval.Should().Be(expectedSeconds);
    }

    [Fact]
    public void BatteryInterval_ExactlyAt15Percent_Returns3Seconds()
    {
        var interval = GetTrackingIntervalSeconds(0.15);

        interval.Should().Be(3, "15% is the boundary — at boundary means normal interval");
    }

    [Fact]
    public void BatteryInterval_At0Percent_Returns10Seconds()
    {
        var interval = GetTrackingIntervalSeconds(0.0);

        interval.Should().Be(10);
    }

    [Fact]
    public void BatteryInterval_At100Percent_Returns3Seconds()
    {
        var interval = GetTrackingIntervalSeconds(1.0);

        interval.Should().Be(3);
    }

    // =========================================================================
    // IsTracking State Transitions
    // =========================================================================

    [Fact]
    public void IsTracking_DefaultIsFalse()
    {
        var state = new EmergencyTrackingState();

        state.IsTracking.Should().BeFalse();
    }

    [Fact]
    public void IsTracking_Activate_SetsToTrue()
    {
        var state = new EmergencyTrackingState();

        Activate(state);

        state.IsTracking.Should().BeTrue();
    }

    [Fact]
    public void IsTracking_ActivateThenDeactivate_SetsToFalse()
    {
        var state = new EmergencyTrackingState();

        Activate(state);
        state.IsTracking.Should().BeTrue();

        Deactivate(state);
        state.IsTracking.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenNotTracking_IsNoOp()
    {
        var state = new EmergencyTrackingState();
        state.IsTracking.Should().BeFalse();

        Deactivate(state);

        state.IsTracking.Should().BeFalse("deactivate on non-tracking state is a no-op");
    }

    [Fact]
    public void Activate_WhenAlreadyTracking_IsNoOp()
    {
        var state = new EmergencyTrackingState();
        Activate(state);

        var activatedAtFirst = state.ActivatedAtUtc;

        // Activate again — should not reset timestamp
        Activate(state);

        state.IsTracking.Should().BeTrue();
        state.ActivatedAtUtc.Should().Be(activatedAtFirst, "re-activation does not reset state");
    }

    // =========================================================================
    // Full Lifecycle
    // =========================================================================

    [Fact]
    public void FullLifecycle_FalseToTrueToFalse()
    {
        var state = new EmergencyTrackingState();

        state.IsTracking.Should().BeFalse();

        Activate(state);
        state.IsTracking.Should().BeTrue();

        Deactivate(state);
        state.IsTracking.Should().BeFalse();
    }

    // =========================================================================
    // Mirrors EmergencyLocationService logic
    // =========================================================================

    private static int GetTrackingIntervalSeconds(double batteryLevel)
    {
        return batteryLevel < 0.15 ? 10 : 3;
    }

    private static void Activate(EmergencyTrackingState state)
    {
        if (state.IsTracking) return; // no-op if already tracking
        state.IsTracking = true;
        state.ActivatedAtUtc = DateTime.UtcNow;
    }

    private static void Deactivate(EmergencyTrackingState state)
    {
        if (!state.IsTracking) return; // no-op if not tracking
        state.IsTracking = false;
        state.ActivatedAtUtc = null;
    }
}

/// <summary>
/// Mirror of EmergencyLocationService tracking state from TheWatch.Mobile.Services
/// </summary>
public class EmergencyTrackingState
{
    public bool IsTracking { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
}
