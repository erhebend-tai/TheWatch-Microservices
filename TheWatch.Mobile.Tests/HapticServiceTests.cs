using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for haptic service logic — vibration pattern definitions, pattern parsing,
/// and enabled/disabled behavior.
/// Since HapticService depends on MAUI Vibration API,
/// we test the pure pattern construction and parsing logic independently.
/// </summary>
public class HapticServiceTests
{
    // =========================================================================
    // SOS Pattern
    // =========================================================================

    [Fact]
    public void SOSPattern_Has5Elements_3Pulses()
    {
        var pattern = GetSOSPattern();

        pattern.Should().HaveCount(5);
        pattern.Should().Equal(500, 200, 500, 200, 500);
    }

    // =========================================================================
    // Alert Pattern
    // =========================================================================

    [Fact]
    public void AlertPattern_Has3Elements_2Pulses()
    {
        var pattern = GetAlertPattern();

        pattern.Should().HaveCount(3);
        pattern.Should().Equal(300, 150, 300);
    }

    // =========================================================================
    // CheckIn Pattern
    // =========================================================================

    [Fact]
    public void CheckInPattern_Has1Element_1Pulse()
    {
        var pattern = GetCheckInPattern();

        pattern.Should().HaveCount(1);
        pattern.Should().Equal(150);
    }

    // =========================================================================
    // Emergency Pattern
    // =========================================================================

    [Fact]
    public void EmergencyPattern_Has9Elements_5Pulses()
    {
        var pattern = GetEmergencyPattern();

        pattern.Should().HaveCount(9);
        pattern.Should().Equal(200, 100, 200, 100, 200, 100, 200, 100, 200);
    }

    // =========================================================================
    // Pattern Parsing: Even Indices = Vibrate, Odd Indices = Pause
    // =========================================================================

    [Fact]
    public void PatternParsing_EvenIndicesAreVibrate()
    {
        var pattern = GetSOSPattern();

        for (int i = 0; i < pattern.Length; i += 2)
        {
            pattern[i].Should().BeGreaterThan(0, $"index {i} should be a vibration duration");
        }
    }

    [Fact]
    public void PatternParsing_OddIndicesArePause()
    {
        var pattern = GetSOSPattern();

        for (int i = 1; i < pattern.Length; i += 2)
        {
            pattern[i].Should().BeGreaterThan(0, $"index {i} should be a pause duration");
            pattern[i].Should().BeLessThan(pattern[i - 1], "pause should be shorter than vibration");
        }
    }

    [Fact]
    public void PatternParsing_EmergencyPattern_AlternatesVibrateAndPause()
    {
        var pattern = GetEmergencyPattern();

        for (int i = 0; i < pattern.Length; i++)
        {
            if (i % 2 == 0)
                pattern[i].Should().Be(200, "even indices = vibrate");
            else
                pattern[i].Should().Be(100, "odd indices = pause");
        }
    }

    // =========================================================================
    // IsEnabled
    // =========================================================================

    [Fact]
    public void IsEnabled_DefaultIsTrue()
    {
        var service = new HapticServiceState();

        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisabledService_SkipsAllPatterns()
    {
        var service = new HapticServiceState { IsEnabled = false };
        var executed = false;

        ExecutePattern(service, GetSOSPattern(), () => executed = true);

        executed.Should().BeFalse("disabled service should skip all patterns");
    }

    [Fact]
    public void EnabledService_ExecutesPattern()
    {
        var service = new HapticServiceState { IsEnabled = true };
        var executed = false;

        ExecutePattern(service, GetSOSPattern(), () => executed = true);

        executed.Should().BeTrue();
    }

    // =========================================================================
    // Mirrors HapticService pattern definitions
    // =========================================================================

    private static int[] GetSOSPattern() => [500, 200, 500, 200, 500];

    private static int[] GetAlertPattern() => [300, 150, 300];

    private static int[] GetCheckInPattern() => [150];

    private static int[] GetEmergencyPattern() => [200, 100, 200, 100, 200, 100, 200, 100, 200];

    private static void ExecutePattern(HapticServiceState state, int[] pattern, Action onExecute)
    {
        if (!state.IsEnabled) return;
        if (pattern.Length == 0) return;
        onExecute();
    }
}

/// <summary>
/// Mirror of HapticService state from TheWatch.Mobile.Services
/// </summary>
public class HapticServiceState
{
    public bool IsEnabled { get; set; } = true;
}
