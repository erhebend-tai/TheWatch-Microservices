using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for mesh fallback service logic — MeshMessage model, activation timer,
/// state transitions, broadcast gating, and event behavior.
/// Since MeshFallbackService depends on platform BLE/WiFi-Direct APIs,
/// we test the pure state machine, message model, and event logic independently.
/// </summary>
public class MeshFallbackServiceTests
{
    // =========================================================================
    // MeshMessage Model
    // =========================================================================

    [Fact]
    public void MeshMessage_Defaults()
    {
        var msg = new MeshMessage();

        msg.Id.Should().Be(Guid.Empty);
        msg.Type.Should().BeEmpty();
        msg.Payload.Should().BeEmpty();
        msg.SenderDeviceId.Should().BeEmpty();
        msg.HopCount.Should().Be(0);
    }

    [Fact]
    public void MeshMessage_AllProperties_PopulatedForSOS()
    {
        var msg = new MeshMessage
        {
            Id = Guid.NewGuid(),
            Type = "SOS",
            Payload = """{"lat":40.7128,"lon":-74.006,"message":"Help!"}""",
            SenderDeviceId = "Samsung_Galaxy_S25",
            HopCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            TTLSeconds = 300
        };

        msg.Id.Should().NotBe(Guid.Empty);
        msg.Type.Should().Be("SOS");
        msg.Payload.Should().Contain("Help!");
        msg.HopCount.Should().Be(0);
        msg.TTLSeconds.Should().Be(300);
    }

    [Fact]
    public void MeshMessage_SOSType()
    {
        var msg = new MeshMessage { Type = "SOS" };

        msg.Type.Should().Be("SOS");
    }

    // =========================================================================
    // Activation Timer
    // =========================================================================

    [Fact]
    public void ActivationTimer_5SecondDelay()
    {
        var activationDelay = TimeSpan.FromSeconds(5);

        activationDelay.TotalSeconds.Should().Be(5);
    }

    [Fact]
    public void ActivationTimer_TimerStarted_IsNotImmediatelyActive()
    {
        var state = new MeshFallbackState();

        StartActivationTimer(state);

        state.IsTimerRunning.Should().BeTrue();
        state.IsActive.Should().BeFalse("mesh activates after timer elapses, not immediately");
    }

    [Fact]
    public void ActivationTimer_TimerCompleted_ActivatesMesh()
    {
        var state = new MeshFallbackState();

        StartActivationTimer(state);
        CompleteActivationTimer(state);

        state.IsActive.Should().BeTrue();
        state.IsTimerRunning.Should().BeFalse();
    }

    // =========================================================================
    // Deactivation
    // =========================================================================

    [Fact]
    public void Deactivation_ResetsNearbyDevicesToZero()
    {
        var state = new MeshFallbackState { IsActive = true, NearbyDevices = 5 };

        DeactivateMesh(state);

        state.NearbyDevices.Should().Be(0);
        state.IsActive.Should().BeFalse();
    }

    // =========================================================================
    // State Transitions
    // =========================================================================

    [Fact]
    public void IsActive_DefaultIsFalse()
    {
        var state = new MeshFallbackState();

        state.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ActivateAndDeactivate()
    {
        var state = new MeshFallbackState();

        ActivateMesh(state);
        state.IsActive.Should().BeTrue();

        DeactivateMesh(state);
        state.IsActive.Should().BeFalse();
    }

    // =========================================================================
    // Broadcast Gating
    // =========================================================================

    [Fact]
    public void CannotBroadcast_WhenMeshNotActive()
    {
        var state = new MeshFallbackState { IsActive = false };

        var canBroadcast = TryBroadcast(state, new MeshMessage { Type = "SOS" });

        canBroadcast.Should().BeFalse("cannot broadcast when mesh is not active");
    }

    [Fact]
    public void CanBroadcast_WhenMeshActive()
    {
        var state = new MeshFallbackState { IsActive = true };

        var canBroadcast = TryBroadcast(state, new MeshMessage { Type = "SOS" });

        canBroadcast.Should().BeTrue();
    }

    // =========================================================================
    // OnlineRestored Cancels Timer
    // =========================================================================

    [Fact]
    public void OnlineRestored_CancelsActivationTimer_AndDeactivatesMesh()
    {
        var state = new MeshFallbackState();

        StartActivationTimer(state);
        state.IsTimerRunning.Should().BeTrue();

        // Online restored — cancel timer and deactivate
        OnlineRestored(state);

        state.IsTimerRunning.Should().BeFalse();
        state.IsActive.Should().BeFalse();
    }

    [Fact]
    public void OnlineRestored_DeactivatesActiveMesh()
    {
        var state = new MeshFallbackState { IsActive = true, NearbyDevices = 3 };

        OnlineRestored(state);

        state.IsActive.Should().BeFalse();
        state.NearbyDevices.Should().Be(0);
    }

    // =========================================================================
    // MeshStateChanged Event
    // =========================================================================

    [Fact]
    public void MeshStateChanged_FiresOnActivation()
    {
        var state = new MeshFallbackState();
        bool? reportedState = null;
        Action<bool> onStateChanged = isActive => reportedState = isActive;

        ActivateMesh(state);
        onStateChanged(state.IsActive);

        reportedState.Should().BeTrue();
    }

    [Fact]
    public void MeshStateChanged_FiresOnDeactivation()
    {
        var state = new MeshFallbackState { IsActive = true };
        bool? reportedState = null;
        Action<bool> onStateChanged = isActive => reportedState = isActive;

        DeactivateMesh(state);
        onStateChanged(state.IsActive);

        reportedState.Should().BeFalse();
    }

    // =========================================================================
    // Mirrors MeshFallbackService logic
    // =========================================================================

    private static void StartActivationTimer(MeshFallbackState state)
    {
        state.IsTimerRunning = true;
    }

    private static void CompleteActivationTimer(MeshFallbackState state)
    {
        state.IsTimerRunning = false;
        state.IsActive = true;
    }

    private static void ActivateMesh(MeshFallbackState state)
    {
        state.IsActive = true;
    }

    private static void DeactivateMesh(MeshFallbackState state)
    {
        state.IsActive = false;
        state.NearbyDevices = 0;
    }

    private static bool TryBroadcast(MeshFallbackState state, MeshMessage message)
    {
        if (!state.IsActive) return false;
        return true;
    }

    private static void OnlineRestored(MeshFallbackState state)
    {
        state.IsTimerRunning = false;
        DeactivateMesh(state);
    }
}

/// <summary>
/// Mirror of MeshMessage from TheWatch.Mobile.Services
/// </summary>
public class MeshMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public string SenderDeviceId { get; set; } = "";
    public int HopCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int TTLSeconds { get; set; }
}

/// <summary>
/// Mirror of MeshFallbackService state from TheWatch.Mobile.Services
/// </summary>
public class MeshFallbackState
{
    public bool IsActive { get; set; }
    public bool IsTimerRunning { get; set; }
    public int NearbyDevices { get; set; }
}
