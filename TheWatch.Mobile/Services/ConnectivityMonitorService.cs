using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Monitors network connectivity changes using MAUI Essentials.
/// Provides events for online/offline transitions and current connectivity status.
/// </summary>
public interface IConnectivityMonitorService
{
    bool IsOnline { get; }
    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
    event EventHandler? OnlineRestored;
    event EventHandler? OfflineDetected;
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
}

public class ConnectivityMonitorService : IConnectivityMonitorService, IDisposable
{
    private readonly ILogger<ConnectivityMonitorService> _logger;
    private bool _isMonitoring;
    private bool _wasOnline = true;
    private Timer? _connectivityTimer;

    public bool IsOnline => Connectivity.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
    public event EventHandler? OnlineRestored;
    public event EventHandler? OfflineDetected;

    public ConnectivityMonitorService(ILogger<ConnectivityMonitorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Start monitoring connectivity changes
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        _wasOnline = IsOnline;

        // Subscribe to MAUI Essentials connectivity events
        Connectivity.ConnectivityChanged += OnConnectivityChanged;

        // Also poll periodically as a backup (some platforms may not fire events reliably)
        _connectivityTimer = new Timer(CheckConnectivityPeriodically, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _logger.LogInformation("Started connectivity monitoring. Current status: {Status}", 
            IsOnline ? "Online" : "Offline");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop monitoring connectivity changes
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
            return;

        _isMonitoring = false;

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _connectivityTimer?.Dispose();
        _connectivityTimer = null;

        _logger.LogInformation("Stopped connectivity monitoring");

        await Task.CompletedTask;
    }

    private void OnConnectivityChanged(object? sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
    {
        if (!_isMonitoring)
            return;

        var isCurrentlyOnline = e.NetworkAccess == NetworkAccess.Internet;
        HandleConnectivityChange(isCurrentlyOnline);

        // Forward as our custom event args
        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(
            e.NetworkAccess, e.ConnectionProfiles));
    }

    private void CheckConnectivityPeriodically(object? state)
    {
        if (!_isMonitoring)
            return;

        var isCurrentlyOnline = IsOnline;
        HandleConnectivityChange(isCurrentlyOnline);
    }

    private void HandleConnectivityChange(bool isCurrentlyOnline)
    {
        // Only fire events on actual state changes
        if (isCurrentlyOnline != _wasOnline)
        {
            _wasOnline = isCurrentlyOnline;

            if (isCurrentlyOnline)
            {
                _logger.LogInformation("Network connectivity restored");
                OnlineRestored?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _logger.LogWarning("Network connectivity lost");
                OfflineDetected?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Dispose()
    {
        _ = StopMonitoringAsync();
        _connectivityTimer?.Dispose();
    }
}

/// <summary>
/// Event args for connectivity state changes
/// </summary>
public class ConnectivityChangedEventArgs : EventArgs
{
    public NetworkAccess NetworkAccess { get; }
    public IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

    public ConnectivityChangedEventArgs(NetworkAccess networkAccess, IEnumerable<ConnectionProfile> connectionProfiles)
    {
        NetworkAccess = networkAccess;
        ConnectionProfiles = connectionProfiles;
    }
}

/// <summary>
/// Extension methods for connectivity monitoring
/// </summary>
public static class ConnectivityExtensions
{
    /// <summary>
    /// Check if device has any network connection (not necessarily internet)
    /// </summary>
    public static bool HasNetworkConnection(this IConnectivityMonitorService connectivity)
    {
        return Connectivity.NetworkAccess != NetworkAccess.None;
    }

    /// <summary>
    /// Get detailed connection information
    /// </summary>
    public static string GetConnectionDetails(this IConnectivityMonitorService connectivity)
    {
        var profiles = Connectivity.ConnectionProfiles;
        var access = Connectivity.NetworkAccess;
        
        return $"Access: {access}, Profiles: [{string.Join(", ", profiles)}]";
    }

    /// <summary>
    /// Check if device is on a metered connection (cellular, limited WiFi)
    /// </summary>
    public static bool IsMeteredConnection(this IConnectivityMonitorService connectivity)
    {
        var profiles = Connectivity.ConnectionProfiles;
        return profiles.Contains(ConnectionProfile.Cellular) || 
               profiles.Contains(ConnectionProfile.Unknown);
    }
}