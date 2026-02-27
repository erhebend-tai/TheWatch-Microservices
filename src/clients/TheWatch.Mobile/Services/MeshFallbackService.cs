using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Activates BLE mesh fallback when device loses internet connectivity for > 5 seconds.
/// Broadcasts SOS messages as BLE advertisements containing incident type, location, and user ID.
/// Matches P3 MeshNetwork message format.
/// Note: Requires Plugin.BLE NuGet package for full BLE implementation.
/// This implementation provides the service framework; BLE scanning/advertising
/// requires platform-specific Bluetooth permissions and hardware support.
/// </summary>
public class MeshFallbackService : IDisposable
{
    private readonly IConnectivityMonitorService _connectivity;
    private readonly ILogger<MeshFallbackService> _logger;
    private CancellationTokenSource? _activationCts;
    private bool _isActive;

    public bool IsActive => _isActive;
    public int NearbyDevices { get; private set; }
    public event Action<bool>? OnMeshStateChanged;
    public event Action<MeshMessage>? OnMeshMessageReceived;

    public MeshFallbackService(
        IConnectivityMonitorService connectivity,
        ILogger<MeshFallbackService> logger)
    {
        _connectivity = connectivity;
        _logger = logger;

        _connectivity.OfflineDetected += OnOfflineDetected;
        _connectivity.OnlineRestored += OnOnlineRestored;
    }

    private void OnOfflineDetected(object? sender, EventArgs e)
    {
        // Wait 5 seconds before activating mesh — might be a brief connectivity blip
        _activationCts?.Cancel();
        _activationCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _activationCts.Token);
                if (!_connectivity.IsOnline)
                {
                    ActivateMesh();
                }
            }
            catch (OperationCanceledException)
            {
                // Connectivity restored before timeout
            }
        });
    }

    private void OnOnlineRestored(object? sender, EventArgs e)
    {
        _activationCts?.Cancel();
        if (_isActive)
        {
            DeactivateMesh();
        }
    }

    private void ActivateMesh()
    {
        if (_isActive) return;

        _isActive = true;
        _logger.LogWarning("Mesh fallback ACTIVATED — device offline, switching to BLE mesh");
        OnMeshStateChanged?.Invoke(true);

        // In production: start BLE scanning and advertising via Plugin.BLE
        // For now, log and set state
    }

    private void DeactivateMesh()
    {
        if (!_isActive) return;

        _isActive = false;
        NearbyDevices = 0;
        _logger.LogInformation("Mesh fallback deactivated — connectivity restored");
        OnMeshStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Broadcast an SOS message over BLE mesh.
    /// Message format matches P3 MeshNetwork: type, location, userId, timestamp.
    /// </summary>
    public async Task BroadcastSOSAsync(string incidentType, double lat, double lon, Guid userId)
    {
        if (!_isActive)
        {
            _logger.LogWarning("Cannot broadcast SOS — mesh not active");
            return;
        }

        var message = new MeshMessage
        {
            Type = "SOS",
            IncidentType = incidentType,
            Latitude = lat,
            Longitude = lon,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            DeviceId = DeviceInfo.Current.Name
        };

        _logger.LogWarning("Broadcasting SOS via mesh: {Type} at ({Lat}, {Lon})",
            incidentType, lat, lon);

        // In production: encode message and broadcast via BLE advertising
        // Plugin.BLE: IAdapter.StartScanningForDevicesAsync() + BLE GATT write
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _connectivity.OfflineDetected -= OnOfflineDetected;
        _connectivity.OnlineRestored -= OnOnlineRestored;
        _activationCts?.Cancel();
        _activationCts?.Dispose();
    }
}

public class MeshMessage
{
    public string Type { get; set; } = "";
    public string IncidentType { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = "";
}
