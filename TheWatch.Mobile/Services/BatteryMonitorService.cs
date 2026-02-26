namespace TheWatch.Mobile.Services;

/// <summary>
/// Monitors battery level and provides different listening modes based on battery status.
/// Helps conserve battery during emergency listening scenarios.
/// </summary>
public class BatteryMonitorService : IDisposable
{
    private bool _isMonitoring;
    private Timer? _batteryCheckTimer;

    public BatteryListeningMode CurrentMode { get; private set; } = BatteryListeningMode.FullListening;
    public double CurrentBatteryLevel { get; private set; } = 1.0;

    public event Action<BatteryListeningMode>? OnModeChanged;
    public event Action<double>? OnBatteryLevelChanged;

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        
        // Subscribe to battery changes
        Battery.BatteryInfoChanged += OnBatteryInfoChanged;
        
        // Start periodic battery check (every 30 seconds)
        _batteryCheckTimer = new Timer(CheckBatteryLevel, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        Battery.BatteryInfoChanged -= OnBatteryInfoChanged;
        _batteryCheckTimer?.Dispose();
        _batteryCheckTimer = null;
    }

    private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
    {
        UpdateBatteryStatus(e.ChargeLevel);
    }

    private void CheckBatteryLevel(object? state)
    {
        try
        {
            var batteryLevel = Battery.ChargeLevel;
            UpdateBatteryStatus(batteryLevel);
        }
        catch (Exception)
        {
            // Ignore battery check errors
        }
    }

    private void UpdateBatteryStatus(double batteryLevel)
    {
        CurrentBatteryLevel = batteryLevel;
        OnBatteryLevelChanged?.Invoke(batteryLevel);

        var newMode = batteryLevel switch
        {
            < 0.10 => BatteryListeningMode.MinimalListening,  // < 10%
            < 0.20 => BatteryListeningMode.ReducedListening, // < 20%
            _ => BatteryListeningMode.FullListening
        };

        if (newMode != CurrentMode)
        {
            CurrentMode = newMode;
            OnModeChanged?.Invoke(newMode);
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

/// <summary>
/// Different listening modes based on battery level.
/// </summary>
public enum BatteryListeningMode
{
    /// <summary>
    /// Continuous recognition (battery > 20%).
    /// </summary>
    FullListening,

    /// <summary>
    /// 5 seconds on, 10 seconds off (battery 10-20%).
    /// </summary>
    ReducedListening,

    /// <summary>
    /// Keyword-only, 2 seconds on, 30 seconds off (battery < 10%).
    /// </summary>
    MinimalListening
}