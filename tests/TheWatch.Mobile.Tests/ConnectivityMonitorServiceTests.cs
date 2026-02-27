using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for connectivity monitoring logic — state transitions, event firing,
/// debouncing, and connection detail formatting.
/// Since ConnectivityMonitorService depends on MAUI Connectivity API,
/// we test the pure state-machine logic and event behavior independently.
/// </summary>
public class ConnectivityMonitorServiceTests
{
    // =========================================================================
    // State Transitions
    // =========================================================================

    [Fact]
    public void HandleConnectivityChange_OnlineToOffline_FiresOfflineDetected()
    {
        var state = new ConnectivityState { CurrentAccess = TestNetworkAccess.Internet };
        var offlineDetected = false;
        Action onOfflineDetected = () => offlineDetected = true;

        HandleConnectivityChange(state, TestNetworkAccess.None, onOfflineDetected, () => { });

        offlineDetected.Should().BeTrue();
        state.CurrentAccess.Should().Be(TestNetworkAccess.None);
    }

    [Fact]
    public void HandleConnectivityChange_OfflineToOnline_FiresOnlineRestored()
    {
        var state = new ConnectivityState { CurrentAccess = TestNetworkAccess.None };
        var onlineRestored = false;
        Action onOnlineRestored = () => onlineRestored = true;

        HandleConnectivityChange(state, TestNetworkAccess.Internet, () => { }, onOnlineRestored);

        onlineRestored.Should().BeTrue();
        state.CurrentAccess.Should().Be(TestNetworkAccess.Internet);
    }

    [Fact]
    public void HandleConnectivityChange_NoEvent_WhenStateUnchanged()
    {
        var state = new ConnectivityState { CurrentAccess = TestNetworkAccess.Internet };
        var eventFired = false;
        Action onEvent = () => eventFired = true;

        HandleConnectivityChange(state, TestNetworkAccess.Internet, onEvent, onEvent);

        eventFired.Should().BeFalse();
    }

    [Fact]
    public void HandleConnectivityChange_Debounce_MultipleSameStateChanges()
    {
        var state = new ConnectivityState { CurrentAccess = TestNetworkAccess.Internet };
        var offlineCount = 0;
        Action onOfflineDetected = () => offlineCount++;

        // First transition fires
        HandleConnectivityChange(state, TestNetworkAccess.None, onOfflineDetected, () => { });
        // Same state again — should not fire
        HandleConnectivityChange(state, TestNetworkAccess.None, onOfflineDetected, () => { });
        // Same state again — should not fire
        HandleConnectivityChange(state, TestNetworkAccess.None, onOfflineDetected, () => { });

        offlineCount.Should().Be(1, "debounce: only first transition fires");
    }

    // =========================================================================
    // Connection Details Formatting
    // =========================================================================

    [Fact]
    public void ConnectionDetails_FormatsCorrectly()
    {
        var access = TestNetworkAccess.Internet;
        var connectionType = "WiFi";

        var details = FormatConnectionDetails(access, connectionType);

        details.Should().Be("Internet via WiFi");
    }

    [Fact]
    public void ConnectionDetails_Offline_FormatsCorrectly()
    {
        var access = TestNetworkAccess.None;
        var connectionType = "None";

        var details = FormatConnectionDetails(access, connectionType);

        details.Should().Be("None via None");
    }

    [Fact]
    public void ConnectionDetails_ConstrainedInternet_FormatsCorrectly()
    {
        var access = TestNetworkAccess.ConstrainedInternet;
        var connectionType = "Cellular";

        var details = FormatConnectionDetails(access, connectionType);

        details.Should().Be("ConstrainedInternet via Cellular");
    }

    // =========================================================================
    // Enum Tests
    // =========================================================================

    [Fact]
    public void TestNetworkAccess_HasFourValues()
    {
        Enum.GetValues<TestNetworkAccess>().Should().HaveCount(4);
    }

    [Fact]
    public void TestNetworkAccess_AllDefined()
    {
        TestNetworkAccess.None.Should().BeDefined();
        TestNetworkAccess.Local.Should().BeDefined();
        TestNetworkAccess.ConstrainedInternet.Should().BeDefined();
        TestNetworkAccess.Internet.Should().BeDefined();
    }

    // =========================================================================
    // ConnectivityState Model
    // =========================================================================

    [Fact]
    public void ConnectivityState_Defaults()
    {
        var state = new ConnectivityState();

        state.CurrentAccess.Should().Be(TestNetworkAccess.None);
        state.LastChangedUtc.Should().Be(default);
    }

    [Fact]
    public void ConnectivityState_TracksLastChanged()
    {
        var state = new ConnectivityState();
        var now = DateTime.UtcNow;

        state.CurrentAccess = TestNetworkAccess.Internet;
        state.LastChangedUtc = now;

        state.LastChangedUtc.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    // =========================================================================
    // Mirrors ConnectivityMonitorService logic
    // =========================================================================

    private static void HandleConnectivityChange(
        ConnectivityState state,
        TestNetworkAccess newAccess,
        Action onOfflineDetected,
        Action onOnlineRestored)
    {
        if (state.CurrentAccess == newAccess) return; // debounce

        var wasOnline = state.CurrentAccess is TestNetworkAccess.Internet or TestNetworkAccess.ConstrainedInternet;
        var isNowOnline = newAccess is TestNetworkAccess.Internet or TestNetworkAccess.ConstrainedInternet;

        state.CurrentAccess = newAccess;
        state.LastChangedUtc = DateTime.UtcNow;

        if (wasOnline && !isNowOnline)
            onOfflineDetected();
        else if (!wasOnline && isNowOnline)
            onOnlineRestored();
    }

    private static string FormatConnectionDetails(TestNetworkAccess access, string connectionType)
    {
        return $"{access} via {connectionType}";
    }
}

/// <summary>
/// Mirror of NetworkAccess from MAUI Connectivity API
/// </summary>
public enum TestNetworkAccess
{
    None,
    Local,
    ConstrainedInternet,
    Internet
}

/// <summary>
/// Mirror of connectivity state tracking from ConnectivityMonitorService
/// </summary>
public class ConnectivityState
{
    public TestNetworkAccess CurrentAccess { get; set; }
    public DateTime LastChangedUtc { get; set; }
}
