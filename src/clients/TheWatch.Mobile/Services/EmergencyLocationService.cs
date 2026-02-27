using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// High-accuracy GPS mode during active emergencies.
/// Switches from GetLastKnownLocation to continuous GetLocationAsync with Best accuracy.
/// Streams location updates every 3 seconds during active emergencies.
/// Respects battery throttling — drops to 10s interval when battery < 15%.
/// </summary>
public class EmergencyLocationService : IDisposable
{
    private readonly BatteryMonitorService _battery;
    private readonly ILogger<EmergencyLocationService> _logger;
    private CancellationTokenSource? _cts;
    private bool _isTracking;

    public bool IsTracking => _isTracking;
    public Location? LastLocation { get; private set; }
    public event Action<Location>? OnLocationUpdated;

    public EmergencyLocationService(
        BatteryMonitorService battery,
        ILogger<EmergencyLocationService> logger)
    {
        _battery = battery;
        _logger = logger;
    }

    /// <summary>
    /// Activate high-accuracy GPS tracking. Call when SOS is triggered.
    /// </summary>
    public async Task ActivateAsync()
    {
        if (_isTracking) return;

        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            _logger.LogWarning("Location permission denied");
            return;
        }

        _isTracking = true;
        _cts = new CancellationTokenSource();

        _logger.LogWarning("Emergency GPS tracking ACTIVATED");
        _ = TrackingLoopAsync(_cts.Token);
    }

    /// <summary>
    /// Deactivate tracking. Call when incident is resolved.
    /// </summary>
    public void Deactivate()
    {
        _isTracking = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _logger.LogInformation("Emergency GPS tracking deactivated");
    }

    /// <summary>
    /// Get a single high-accuracy location reading.
    /// </summary>
    public async Task<Location?> GetHighAccuracyLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
            var location = await Geolocation.GetLocationAsync(request);
            if (location is not null)
            {
                LastLocation = location;
            }
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get high-accuracy location");
            // Fall back to last known
            return await Geolocation.GetLastKnownLocationAsync();
        }
    }

    private async Task TrackingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isTracking)
        {
            try
            {
                var location = await GetHighAccuracyLocationAsync();
                if (location is not null)
                {
                    LastLocation = location;
                    OnLocationUpdated?.Invoke(location);
                }

                // Interval depends on battery level
                var interval = _battery.CurrentBatteryLevel < 0.15
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(3);

                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in emergency location tracking loop");
                await Task.Delay(5000, ct);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
