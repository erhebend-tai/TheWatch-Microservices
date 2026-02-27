using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for SignalR service logic — exponential backoff calculation,
/// hub entity name mapping, and event record model creation.
/// Since WatchSignalRService depends on Microsoft.AspNetCore.SignalR.Client,
/// we test the pure retry/mapping logic and event models independently.
/// </summary>
public class WatchSignalRServiceTests
{
    private const int MaxRetries = 8;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    // =========================================================================
    // Exponential Backoff
    // =========================================================================

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    public void ExponentialBackoff_ReturnsExpectedSeconds(int retryNumber, int expectedSeconds)
    {
        var delay = CalculateRetryDelay(retryNumber);

        delay.Should().NotBeNull();
        delay!.Value.TotalSeconds.Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(6, 30)]
    [InlineData(7, 30)]
    public void ExponentialBackoff_CappedAtMaxDelay(int retryNumber, int expectedSeconds)
    {
        var delay = CalculateRetryDelay(retryNumber);

        delay.Should().NotBeNull();
        delay!.Value.TotalSeconds.Should().Be(expectedSeconds);
    }

    [Fact]
    public void ExponentialBackoff_MaxRetriesReached_ReturnsNull()
    {
        var delay = CalculateRetryDelay(MaxRetries);

        delay.Should().BeNull("max retries reached, should give up");
    }

    [Fact]
    public void ExponentialBackoff_BeyondMaxRetries_ReturnsNull()
    {
        var delay = CalculateRetryDelay(MaxRetries + 1);

        delay.Should().BeNull();
    }

    // =========================================================================
    // Hub Entity Name Mapping
    // =========================================================================

    [Theory]
    [InlineData("incidents", "Incident")]
    [InlineData("dispatches", "Dispatch")]
    [InlineData("responders", "Responder")]
    [InlineData("checkins", "CheckIn")]
    [InlineData("vitalreadings", "VitalReading")]
    [InlineData("medicalalerts", "MedicalAlert")]
    public void HubEntityMapping_KnownEntities_ReturnsMappedName(string hubName, string expected)
    {
        var mapped = MapHubEntityName(hubName);

        mapped.Should().Be(expected);
    }

    [Fact]
    public void HubEntityMapping_UnknownEntity_PassesThrough()
    {
        var mapped = MapHubEntityName("unknownentity");

        mapped.Should().Be("unknownentity");
    }

    [Fact]
    public void HubEntityMapping_CaseInsensitive()
    {
        var mapped = MapHubEntityName("Incidents");

        // Our mapping uses lowercase keys
        mapped.Should().Be("Incident");
    }

    // =========================================================================
    // Event Records
    // =========================================================================

    [Fact]
    public void IncidentEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "Incident",
            EntityId = Guid.NewGuid(),
            Action = "Created",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("Incident");
        evt.Action.Should().Be("Created");
    }

    [Fact]
    public void DispatchEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "Dispatch",
            EntityId = Guid.NewGuid(),
            Action = "Updated",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("Dispatch");
    }

    [Fact]
    public void ResponderEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "Responder",
            EntityId = Guid.NewGuid(),
            Action = "LocationUpdated",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("Responder");
    }

    [Fact]
    public void CheckInEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "CheckIn",
            EntityId = Guid.NewGuid(),
            Action = "Created",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("CheckIn");
    }

    [Fact]
    public void VitalEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "VitalReading",
            EntityId = Guid.NewGuid(),
            Action = "Created",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("VitalReading");
    }

    [Fact]
    public void AlertEvent_CanBeCreated()
    {
        var evt = new SignalREvent
        {
            EntityType = "MedicalAlert",
            EntityId = Guid.NewGuid(),
            Action = "Triggered",
            ReceivedAtUtc = DateTime.UtcNow
        };

        evt.EntityType.Should().Be("MedicalAlert");
    }

    [Fact]
    public void SignalREvent_Defaults()
    {
        var evt = new SignalREvent();

        evt.EntityType.Should().BeEmpty();
        evt.EntityId.Should().Be(Guid.Empty);
        evt.Action.Should().BeEmpty();
    }

    // =========================================================================
    // Mirrors WatchSignalRService logic
    // =========================================================================

    private static TimeSpan? CalculateRetryDelay(int retryNumber)
    {
        if (retryNumber >= MaxRetries) return null;

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryNumber));
        return delay > MaxDelay ? MaxDelay : delay;
    }

    private static string MapHubEntityName(string hubName)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["incidents"] = "Incident",
            ["dispatches"] = "Dispatch",
            ["responders"] = "Responder",
            ["checkins"] = "CheckIn",
            ["vitalreadings"] = "VitalReading",
            ["medicalalerts"] = "MedicalAlert"
        };

        return mapping.TryGetValue(hubName, out var mapped) ? mapped : hubName;
    }
}

/// <summary>
/// Mirror of SignalR event record from TheWatch.Mobile.Services
/// </summary>
public class SignalREvent
{
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "";
    public DateTime ReceivedAtUtc { get; set; }
}
