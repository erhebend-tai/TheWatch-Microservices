using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for battery monitoring mode transitions.
/// Reimplements the battery-level-to-listening-mode logic from BatteryMonitorService.
/// The actual MAUI Battery API isn't available, but the mode calculation is pure logic.
/// </summary>
public class BatteryMonitorTests
{
    // =========================================================================
    // Battery Mode Transitions
    // =========================================================================

    [Theory]
    [InlineData(1.0, BatteryListeningMode.FullListening)]
    [InlineData(0.8, BatteryListeningMode.FullListening)]
    [InlineData(0.5, BatteryListeningMode.FullListening)]
    [InlineData(0.21, BatteryListeningMode.FullListening)]
    [InlineData(0.20, BatteryListeningMode.FullListening)]  // 20% = full (not < 0.20)
    public void GetMode_FullListening_WhenBatteryAbove20Percent(double level, BatteryListeningMode expected)
    {
        var mode = GetListeningMode(level);
        mode.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.19, BatteryListeningMode.ReducedListening)]
    [InlineData(0.15, BatteryListeningMode.ReducedListening)]
    [InlineData(0.10, BatteryListeningMode.ReducedListening)]  // 10% = reduced (not < 0.10)
    public void GetMode_ReducedListening_WhenBattery10To20Percent(double level, BatteryListeningMode expected)
    {
        var mode = GetListeningMode(level);
        mode.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.09, BatteryListeningMode.MinimalListening)]
    [InlineData(0.05, BatteryListeningMode.MinimalListening)]
    [InlineData(0.01, BatteryListeningMode.MinimalListening)]
    [InlineData(0.0, BatteryListeningMode.MinimalListening)]
    public void GetMode_MinimalListening_WhenBatteryBelow10Percent(double level, BatteryListeningMode expected)
    {
        var mode = GetListeningMode(level);
        mode.Should().Be(expected);
    }

    // =========================================================================
    // Mode Change Detection
    // =========================================================================

    [Fact]
    public void ModeChange_DetectsTransitionFromFullToReduced()
    {
        var currentMode = BatteryListeningMode.FullListening;
        var newMode = GetListeningMode(0.15);

        var changed = newMode != currentMode;

        changed.Should().BeTrue();
        newMode.Should().Be(BatteryListeningMode.ReducedListening);
    }

    [Fact]
    public void ModeChange_DetectsTransitionFromReducedToMinimal()
    {
        var currentMode = BatteryListeningMode.ReducedListening;
        var newMode = GetListeningMode(0.05);

        var changed = newMode != currentMode;

        changed.Should().BeTrue();
        newMode.Should().Be(BatteryListeningMode.MinimalListening);
    }

    [Fact]
    public void ModeChange_NoChange_WhenSameRange()
    {
        var currentMode = BatteryListeningMode.FullListening;
        var newMode = GetListeningMode(0.50);

        var changed = newMode != currentMode;

        changed.Should().BeFalse();
    }

    [Fact]
    public void ModeChange_Recovery_MinimalToFull()
    {
        // Simulates charging: battery goes from minimal to full
        var currentMode = BatteryListeningMode.MinimalListening;
        var newMode = GetListeningMode(0.80);

        var changed = newMode != currentMode;

        changed.Should().BeTrue();
        newMode.Should().Be(BatteryListeningMode.FullListening);
    }

    // =========================================================================
    // Event Firing Simulation
    // =========================================================================

    [Fact]
    public void BatteryLevelChanged_EventFires_OnLevelUpdate()
    {
        double? reportedLevel = null;
        Action<double> onLevelChanged = level => reportedLevel = level;

        // Simulate UpdateBatteryStatus
        var batteryLevel = 0.75;
        onLevelChanged(batteryLevel);

        reportedLevel.Should().Be(0.75);
    }

    [Fact]
    public void ModeChanged_EventFires_OnTransition()
    {
        BatteryListeningMode? reportedMode = null;
        Action<BatteryListeningMode> onModeChanged = mode => reportedMode = mode;

        var currentMode = BatteryListeningMode.FullListening;
        var newMode = GetListeningMode(0.08);

        if (newMode != currentMode)
        {
            currentMode = newMode;
            onModeChanged(newMode);
        }

        reportedMode.Should().Be(BatteryListeningMode.MinimalListening);
    }

    [Fact]
    public void ModeChanged_DoesNotFire_WhenModeUnchanged()
    {
        BatteryListeningMode? reportedMode = null;
        Action<BatteryListeningMode> onModeChanged = mode => reportedMode = mode;

        var currentMode = BatteryListeningMode.FullListening;
        var newMode = GetListeningMode(0.50);

        if (newMode != currentMode)
        {
            currentMode = newMode;
            onModeChanged(newMode);
        }

        reportedMode.Should().BeNull(); // Event should not have fired
    }

    // =========================================================================
    // Boundary Tests
    // =========================================================================

    [Fact]
    public void BoundaryTest_ExactlyAt10Percent()
    {
        // 0.10 is NOT < 0.10, so should be ReducedListening
        var mode = GetListeningMode(0.10);
        mode.Should().Be(BatteryListeningMode.ReducedListening);
    }

    [Fact]
    public void BoundaryTest_ExactlyAt20Percent()
    {
        // 0.20 is NOT < 0.20, so should be FullListening
        var mode = GetListeningMode(0.20);
        mode.Should().Be(BatteryListeningMode.FullListening);
    }

    [Fact]
    public void BoundaryTest_JustBelow10()
    {
        var mode = GetListeningMode(0.099);
        mode.Should().Be(BatteryListeningMode.MinimalListening);
    }

    [Fact]
    public void BoundaryTest_JustBelow20()
    {
        var mode = GetListeningMode(0.199);
        mode.Should().Be(BatteryListeningMode.ReducedListening);
    }

    // =========================================================================
    // Enum Tests
    // =========================================================================

    [Fact]
    public void BatteryListeningMode_HasThreeValues()
    {
        Enum.GetValues<BatteryListeningMode>().Should().HaveCount(3);
    }

    [Fact]
    public void BatteryListeningMode_ValuesExist()
    {
        BatteryListeningMode.FullListening.Should().BeDefined();
        BatteryListeningMode.ReducedListening.Should().BeDefined();
        BatteryListeningMode.MinimalListening.Should().BeDefined();
    }

    // =========================================================================
    // Mirrors BatteryMonitorService.UpdateBatteryStatus mode logic
    // =========================================================================

    private static BatteryListeningMode GetListeningMode(double batteryLevel)
    {
        return batteryLevel switch
        {
            < 0.10 => BatteryListeningMode.MinimalListening,
            < 0.20 => BatteryListeningMode.ReducedListening,
            _ => BatteryListeningMode.FullListening
        };
    }
}

/// <summary>
/// Mirror of BatteryListeningMode from TheWatch.Mobile.Services
/// </summary>
public enum BatteryListeningMode
{
    FullListening,
    ReducedListening,
    MinimalListening
}
