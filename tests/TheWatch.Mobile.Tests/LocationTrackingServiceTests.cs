using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for location tracking service logic — battery-based accuracy and interval
/// selection, LocationRecord model, consent gating, buffer thread-safety, and batch upload.
/// Since LocationTrackingService depends on MAUI Geolocation and Battery APIs,
/// we test the pure algorithms and data models independently.
/// </summary>
public class LocationTrackingServiceTests
{
    // =========================================================================
    // Battery-Based Accuracy
    // =========================================================================

    [Theory]
    [InlineData(0.14, LocationAccuracy.Medium)]
    [InlineData(0.10, LocationAccuracy.Medium)]
    [InlineData(0.05, LocationAccuracy.Medium)]
    [InlineData(0.0, LocationAccuracy.Medium)]
    public void BatteryAccuracy_Below15Percent_ReturnsMedium(double batteryLevel, LocationAccuracy expected)
    {
        var accuracy = GetAccuracyForBattery(batteryLevel);

        accuracy.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.15, LocationAccuracy.High)]
    [InlineData(0.50, LocationAccuracy.High)]
    [InlineData(1.0, LocationAccuracy.High)]
    public void BatteryAccuracy_AtOrAbove15Percent_ReturnsHigh(double batteryLevel, LocationAccuracy expected)
    {
        var accuracy = GetAccuracyForBattery(batteryLevel);

        accuracy.Should().Be(expected);
    }

    // =========================================================================
    // Battery-Based Interval
    // =========================================================================

    [Theory]
    [InlineData(0.09, 120)]
    [InlineData(0.05, 120)]
    [InlineData(0.01, 120)]
    public void BatteryInterval_Below10Percent_Returns2Minutes(double batteryLevel, int expectedSeconds)
    {
        var interval = GetIntervalForBattery(batteryLevel);

        interval.Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(0.10, 60)]
    [InlineData(0.15, 60)]
    [InlineData(0.19, 60)]
    public void BatteryInterval_10To20Percent_Returns60Seconds(double batteryLevel, int expectedSeconds)
    {
        var interval = GetIntervalForBattery(batteryLevel);

        interval.Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(0.20, 15)]
    [InlineData(0.50, 15)]
    [InlineData(1.0, 15)]
    public void BatteryInterval_AtOrAbove20Percent_Returns15Seconds(double batteryLevel, int expectedSeconds)
    {
        var interval = GetIntervalForBattery(batteryLevel);

        interval.Should().Be(expectedSeconds);
    }

    // =========================================================================
    // LocationRecord Model
    // =========================================================================

    [Fact]
    public void LocationRecord_Defaults()
    {
        var record = new LocationRecord();

        record.Id.Should().Be(Guid.Empty);
        record.Latitude.Should().Be(0);
        record.Longitude.Should().Be(0);
        record.Accuracy.Should().Be(0);
        record.Uploaded.Should().BeFalse();
    }

    [Fact]
    public void LocationRecord_AllPropertiesCanBeSet()
    {
        var id = Guid.NewGuid();
        var record = new LocationRecord
        {
            Id = id,
            Latitude = 40.7128,
            Longitude = -74.006,
            Accuracy = 5.0,
            Timestamp = DateTime.UtcNow,
            Uploaded = true,
            BatteryLevel = 0.85
        };

        record.Id.Should().Be(id);
        record.Latitude.Should().Be(40.7128);
        record.Longitude.Should().Be(-74.006);
        record.Accuracy.Should().Be(5.0);
        record.Uploaded.Should().BeTrue();
        record.BatteryLevel.Should().Be(0.85);
    }

    // =========================================================================
    // Consent Gating
    // =========================================================================

    [Fact]
    public void HasConsent_DefaultIsFalse()
    {
        var state = new LocationTrackingState();

        state.HasConsent.Should().BeFalse();
    }

    [Fact]
    public void Tracking_NotStarted_WithoutConsent()
    {
        var state = new LocationTrackingState { HasConsent = false };

        var started = TryStartTracking(state);

        started.Should().BeFalse("tracking requires consent");
        state.IsTracking.Should().BeFalse();
    }

    [Fact]
    public void Tracking_Started_WithConsent()
    {
        var state = new LocationTrackingState { HasConsent = true };

        var started = TryStartTracking(state);

        started.Should().BeTrue();
        state.IsTracking.Should().BeTrue();
    }

    // =========================================================================
    // Buffer Thread-Safety
    // =========================================================================

    [Fact]
    public void Buffer_ConcurrentAdds_DoNotCrash()
    {
        var buffer = new ConcurrentBag<LocationRecord>();

        Parallel.For(0, 100, i =>
        {
            buffer.Add(new LocationRecord
            {
                Id = Guid.NewGuid(),
                Latitude = 40.0 + i * 0.001,
                Longitude = -74.0 + i * 0.001,
                Timestamp = DateTime.UtcNow
            });
        });

        buffer.Count.Should().Be(100);
    }

    // =========================================================================
    // Batch Upload
    // =========================================================================

    [Fact]
    public void BatchUpload_ClearsBuffer()
    {
        var buffer = new ConcurrentBag<LocationRecord>();
        for (int i = 0; i < 10; i++)
        {
            buffer.Add(new LocationRecord { Id = Guid.NewGuid() });
        }
        buffer.Count.Should().Be(10);

        // Simulate batch upload — drain the buffer
        var batch = new List<LocationRecord>();
        while (buffer.TryTake(out var record))
        {
            batch.Add(record);
        }

        batch.Should().HaveCount(10);
        buffer.Should().BeEmpty();
    }

    // =========================================================================
    // Mirrors LocationTrackingService logic
    // =========================================================================

    private static LocationAccuracy GetAccuracyForBattery(double batteryLevel)
    {
        return batteryLevel < 0.15 ? LocationAccuracy.Medium : LocationAccuracy.High;
    }

    private static int GetIntervalForBattery(double batteryLevel)
    {
        return batteryLevel switch
        {
            < 0.10 => 120,
            < 0.20 => 60,
            _ => 15
        };
    }

    private static bool TryStartTracking(LocationTrackingState state)
    {
        if (!state.HasConsent) return false;
        state.IsTracking = true;
        return true;
    }
}

/// <summary>
/// Mirror of LocationAccuracy from TheWatch.Mobile.Services
/// </summary>
public enum LocationAccuracy
{
    Low,
    Medium,
    High
}

/// <summary>
/// Mirror of LocationRecord from TheWatch.Mobile.Services
/// </summary>
public class LocationRecord
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Uploaded { get; set; }
    public double BatteryLevel { get; set; }
}

/// <summary>
/// Mirror of LocationTrackingService state from TheWatch.Mobile.Services
/// </summary>
public class LocationTrackingState
{
    public bool HasConsent { get; set; }
    public bool IsTracking { get; set; }
}
