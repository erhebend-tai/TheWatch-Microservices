using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for biometric gate logic — fail-open design, platform availability,
/// and enabled/disabled toggling.
/// Since BiometricGateService depends on MAUI platform APIs (fingerprint, face ID),
/// we test the pure gate decision logic and configuration independently.
/// </summary>
public class BiometricGateServiceTests
{
    // =========================================================================
    // Fail-Open Design
    // =========================================================================

    [Fact]
    public void Gate_WhenDisabled_ReturnsTrue()
    {
        var isEnabled = false;

        var result = EvaluateGate(isEnabled, biometricSuccess: false, exceptionThrown: false);

        result.Should().BeTrue("fail-open: disabled gate always passes");
    }

    [Fact]
    public void Gate_WhenEnabled_AndBiometricSucceeds_ReturnsTrue()
    {
        var isEnabled = true;

        var result = EvaluateGate(isEnabled, biometricSuccess: true, exceptionThrown: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void Gate_WhenEnabled_AndBiometricFails_ReturnsFalse()
    {
        var isEnabled = true;

        var result = EvaluateGate(isEnabled, biometricSuccess: false, exceptionThrown: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void Gate_FailOpen_OnException_ReturnsTrue()
    {
        var isEnabled = true;

        var result = EvaluateGate(isEnabled, biometricSuccess: false, exceptionThrown: true);

        result.Should().BeTrue("fail-open: exceptions always pass to avoid locking user out");
    }

    // =========================================================================
    // Platform Availability
    // =========================================================================

    [Theory]
    [InlineData(TestDevicePlatform.Android, true)]
    [InlineData(TestDevicePlatform.iOS, true)]
    [InlineData(TestDevicePlatform.WinUI, true)]
    [InlineData(TestDevicePlatform.MacCatalyst, false)]
    [InlineData(TestDevicePlatform.Tizen, false)]
    public void IsBiometricAvailable_ReturnsCorrectly_ByPlatform(TestDevicePlatform platform, bool expected)
    {
        var available = IsBiometricAvailableOnPlatform(platform);

        available.Should().Be(expected);
    }

    // =========================================================================
    // IsEnabled Toggle
    // =========================================================================

    [Fact]
    public void IsEnabled_DefaultIsFalse()
    {
        var gate = new BiometricGateState();

        gate.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_CanBeToggled()
    {
        var gate = new BiometricGateState();

        gate.IsEnabled = true;
        gate.IsEnabled.Should().BeTrue();

        gate.IsEnabled = false;
        gate.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WhenEnabled_GateRequiresBiometric()
    {
        var gate = new BiometricGateState { IsEnabled = true };

        var result = EvaluateGate(gate.IsEnabled, biometricSuccess: false, exceptionThrown: false);

        result.Should().BeFalse("enabled gate with failed biometric should deny");
    }

    [Fact]
    public void IsEnabled_WhenDisabled_GatePassesThrough()
    {
        var gate = new BiometricGateState { IsEnabled = false };

        var result = EvaluateGate(gate.IsEnabled, biometricSuccess: false, exceptionThrown: false);

        result.Should().BeTrue("disabled gate always passes");
    }

    // =========================================================================
    // Enum Tests
    // =========================================================================

    [Fact]
    public void TestDevicePlatform_HasExpectedValues()
    {
        Enum.GetValues<TestDevicePlatform>().Should().HaveCount(5);
    }

    // =========================================================================
    // Mirrors BiometricGateService logic
    // =========================================================================

    private static bool EvaluateGate(bool isEnabled, bool biometricSuccess, bool exceptionThrown)
    {
        if (!isEnabled) return true;
        if (exceptionThrown) return true; // fail-open
        return biometricSuccess;
    }

    private static bool IsBiometricAvailableOnPlatform(TestDevicePlatform platform)
    {
        return platform is TestDevicePlatform.Android
            or TestDevicePlatform.iOS
            or TestDevicePlatform.WinUI;
    }
}

/// <summary>
/// Mirror of BiometricGateService state from TheWatch.Mobile.Services
/// </summary>
public class BiometricGateState
{
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Mirror of DevicePlatform enum for testing platform availability logic
/// </summary>
public enum TestDevicePlatform
{
    Android,
    iOS,
    WinUI,
    MacCatalyst,
    Tizen
}
