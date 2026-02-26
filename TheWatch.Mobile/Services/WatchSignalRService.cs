using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Manages SignalR hub connections for the MAUI app.
/// Connects to P2 (incidents/dispatch), P6 (responders), P7 (family health) hubs.
/// Includes automatic reconnection with exponential backoff.
/// </summary>
public class WatchSignalRService : IAsyncDisposable
{
    private readonly WatchAuthService _auth;
    private readonly ILogger<WatchSignalRService> _logger;
    private readonly Dictionary<string, HubConnection> _connections = [];
    private bool _disposed;

    // Base URLs — same as WatchApiClient
    private const string P2Base = "http://localhost:5002";
    private const string P6Base = "http://localhost:5006";
    private const string P7Base = "http://localhost:5007";

    // Events for Blazor pages to subscribe to
    public event Action<IncidentEvent>? OnIncidentCreated;
    public event Action<IncidentEvent>? OnIncidentUpdated;
    public event Action<DispatchEvent>? OnDispatchCreated;
    public event Action<ResponderEvent>? OnResponderUpdated;
    public event Action<CheckInEvent>? OnCheckInReceived;
    public event Action<VitalEvent>? OnVitalReceived;
    public event Action<AlertEvent>? OnAlertReceived;
    public event Action<string, HubConnectionState>? OnConnectionStateChanged;

    public WatchSignalRService(WatchAuthService auth, ILogger<WatchSignalRService> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Connect to all hubs. Call after authentication.
    /// </summary>
    public async Task ConnectAllAsync()
    {
        await ConnectToIncidentHubAsync();
        await ConnectToDispatchHubAsync();
        await ConnectToResponderHubAsync();
        await ConnectToFamilyHubsAsync();
    }

    /// <summary>
    /// Disconnect from all hubs.
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        foreach (var (name, conn) in _connections)
        {
            try
            {
                await conn.StopAsync();
                _logger.LogInformation("Disconnected from {Hub}", name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting from {Hub}", name);
            }
        }
        _connections.Clear();
    }

    public HubConnectionState GetConnectionState(string hubName)
    {
        return _connections.TryGetValue(hubName, out var conn)
            ? conn.State
            : HubConnectionState.Disconnected;
    }

    // === P2 Incident Hub ===

    public async Task ConnectToIncidentHubAsync()
    {
        var conn = await GetOrCreateConnectionAsync("incidents", $"{P2Base}/hubs/incidents");

        conn.On<object>("OnIncidentCreated", entity =>
        {
            OnIncidentCreated?.Invoke(new IncidentEvent("Created", entity));
        });

        conn.On<object>("OnIncidentUpdated", entity =>
        {
            OnIncidentUpdated?.Invoke(new IncidentEvent("Updated", entity));
        });

        await StartConnectionAsync("incidents", conn);
    }

    // === P2 Dispatch Hub ===

    public async Task ConnectToDispatchHubAsync()
    {
        var conn = await GetOrCreateConnectionAsync("dispatches", $"{P2Base}/hubs/dispatches");

        conn.On<object>("OnDispatchCreated", entity =>
        {
            OnDispatchCreated?.Invoke(new DispatchEvent("Created", entity));
        });

        await StartConnectionAsync("dispatches", conn);
    }

    // === P6 Responder Hub ===

    public async Task ConnectToResponderHubAsync()
    {
        var conn = await GetOrCreateConnectionAsync("responders", $"{P6Base}/hubs/responders");

        conn.On<object>("OnResponderUpdated", entity =>
        {
            OnResponderUpdated?.Invoke(new ResponderEvent("Updated", entity));
        });

        await StartConnectionAsync("responders", conn);
    }

    // === P7 Family Hubs ===

    public async Task ConnectToFamilyHubsAsync()
    {
        // Check-ins
        var checkInConn = await GetOrCreateConnectionAsync("checkins", $"{P7Base}/hubs/checkins");
        checkInConn.On<object>("OnCheckInCreated", entity =>
        {
            OnCheckInReceived?.Invoke(new CheckInEvent("Created", entity));
        });
        await StartConnectionAsync("checkins", checkInConn);

        // Vitals
        var vitalConn = await GetOrCreateConnectionAsync("vitalreadings", $"{P7Base}/hubs/vitalreadings");
        vitalConn.On<object>("OnVitalReadingCreated", entity =>
        {
            OnVitalReceived?.Invoke(new VitalEvent("Created", entity));
        });
        await StartConnectionAsync("vitalreadings", vitalConn);

        // Alerts
        var alertConn = await GetOrCreateConnectionAsync("medicalalerts", $"{P7Base}/hubs/medicalalerts");
        alertConn.On<object>("OnMedicalAlertCreated", entity =>
        {
            OnAlertReceived?.Invoke(new AlertEvent("Created", entity));
        });
        alertConn.On<object>("OnMedicalAlertUpdated", entity =>
        {
            OnAlertReceived?.Invoke(new AlertEvent("Updated", entity));
        });
        await StartConnectionAsync("medicalalerts", alertConn);
    }

    // === Group Management ===

    public async Task JoinGroupAsync(string hubName, string groupId)
    {
        if (!_connections.TryGetValue(hubName, out var conn)) return;
        if (conn.State != HubConnectionState.Connected) return;

        var entityName = hubName switch
        {
            "incidents" => "Incident",
            "dispatches" => "Dispatch",
            "responders" => "Responder",
            "checkins" => "CheckIn",
            "vitalreadings" => "VitalReading",
            "medicalalerts" => "MedicalAlert",
            _ => hubName
        };

        await conn.InvokeAsync($"Join{entityName}Group", groupId);
        _logger.LogInformation("Joined group {GroupId} on {Hub}", groupId, hubName);
    }

    public async Task LeaveGroupAsync(string hubName, string groupId)
    {
        if (!_connections.TryGetValue(hubName, out var conn)) return;
        if (conn.State != HubConnectionState.Connected) return;

        var entityName = hubName switch
        {
            "incidents" => "Incident",
            "dispatches" => "Dispatch",
            "responders" => "Responder",
            "checkins" => "CheckIn",
            "vitalreadings" => "VitalReading",
            "medicalalerts" => "MedicalAlert",
            _ => hubName
        };

        await conn.InvokeAsync($"Leave{entityName}Group", groupId);
        _logger.LogInformation("Left group {GroupId} on {Hub}", groupId, hubName);
    }

    // === Connection Management with Exponential Backoff ===

    private Task<HubConnection> GetOrCreateConnectionAsync(string name, string url)
    {
        if (_connections.TryGetValue(name, out var existing))
            return Task.FromResult(existing);

        var conn = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = async () => await _auth.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect(new ExponentialBackoffRetryPolicy(_logger, name))
            .Build();

        conn.Reconnecting += error =>
        {
            _logger.LogWarning("Reconnecting to {Hub}: {Error}", name, error?.Message);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        conn.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected to {Hub}: {ConnectionId}", name, connectionId);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        conn.Closed += error =>
        {
            _logger.LogWarning("Connection to {Hub} closed: {Error}", name, error?.Message);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        _connections[name] = conn;
        return Task.FromResult(conn);
    }

    private async Task StartConnectionAsync(string name, HubConnection conn)
    {
        if (conn.State != HubConnectionState.Disconnected) return;

        try
        {
            await conn.StartAsync();
            _logger.LogInformation("Connected to {Hub} ({State})", name, conn.State);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to {Hub} — will retry on next ConnectAll", name);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Disconnected);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var conn in _connections.Values)
        {
            try { await conn.DisposeAsync(); } catch { }
        }
        _connections.Clear();
    }
}

/// <summary>
/// Exponential backoff retry policy for SignalR reconnection.
/// Delays: 0s, 2s, 4s, 8s, 16s, 30s, 30s, 30s... (max 8 retries then gives up)
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly ILogger _logger;
    private readonly string _hubName;
    private const int MaxRetries = 8;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    public ExponentialBackoffRetryPolicy(ILogger logger, string hubName)
    {
        _logger = logger;
        _hubName = hubName;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= MaxRetries)
        {
            _logger.LogWarning("SignalR {Hub}: max retries ({MaxRetries}) reached, giving up", _hubName, MaxRetries);
            return null; // Stop retrying
        }

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryContext.PreviousRetryCount));
        if (delay > MaxDelay) delay = MaxDelay;

        _logger.LogInformation("SignalR {Hub}: retry #{Attempt} in {Delay}s",
            _hubName, retryContext.PreviousRetryCount + 1, delay.TotalSeconds);

        return delay;
    }
}

// === Event DTOs for Blazor page consumption ===

public record IncidentEvent(string Action, object Data);
public record DispatchEvent(string Action, object Data);
public record ResponderEvent(string Action, object Data);
public record CheckInEvent(string Action, object Data);
public record VitalEvent(string Action, object Data);
public record AlertEvent(string Action, object Data);
