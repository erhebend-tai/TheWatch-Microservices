using Microsoft.AspNetCore.SignalR.Client;

namespace TheWatch.Dashboard.Services;

/// <summary>
/// Manages SignalR hub connections for the Blazor Dashboard.
/// Connects to P2 (incidents/dispatch), P6 (responders), P7 (family health) hubs.
/// Includes automatic reconnection with exponential backoff.
/// </summary>
public class DashboardSignalRService : IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<DashboardSignalRService> _logger;
    private readonly Dictionary<string, HubConnection> _connections = [];
    private bool _disposed;

    // Events for Blazor components
    public event Action<string, object>? OnIncidentEvent;
    public event Action<string, object>? OnDispatchEvent;
    public event Action<string, object>? OnResponderEvent;
    public event Action<string, object>? OnCheckInEvent;
    public event Action<string, object>? OnVitalEvent;
    public event Action<string, object>? OnAlertEvent;
    public event Action<string, HubConnectionState>? OnConnectionStateChanged;

    public DashboardSignalRService(IConfiguration config, ILogger<DashboardSignalRService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Connect to all service hubs.
    /// </summary>
    public async Task ConnectAllAsync()
    {
        await ConnectHubAsync("incidents", "p2-voiceemergency", "/hubs/incidents", conn =>
        {
            conn.On<object>("OnIncidentCreated", e => OnIncidentEvent?.Invoke("Created", e));
            conn.On<object>("OnIncidentUpdated", e => OnIncidentEvent?.Invoke("Updated", e));
            conn.On<object>("OnIncidentDeleted", e => OnIncidentEvent?.Invoke("Deleted", e));
        });

        await ConnectHubAsync("dispatches", "p2-voiceemergency", "/hubs/dispatches", conn =>
        {
            conn.On<object>("OnDispatchCreated", e => OnDispatchEvent?.Invoke("Created", e));
            conn.On<object>("OnDispatchUpdated", e => OnDispatchEvent?.Invoke("Updated", e));
        });

        await ConnectHubAsync("responders", "p6-firstresponder", "/hubs/responders", conn =>
        {
            conn.On<object>("OnResponderCreated", e => OnResponderEvent?.Invoke("Created", e));
            conn.On<object>("OnResponderUpdated", e => OnResponderEvent?.Invoke("Updated", e));
        });

        await ConnectHubAsync("checkins", "p7-familyhealth", "/hubs/checkins", conn =>
        {
            conn.On<object>("OnCheckInCreated", e => OnCheckInEvent?.Invoke("Created", e));
        });

        await ConnectHubAsync("vitalreadings", "p7-familyhealth", "/hubs/vitalreadings", conn =>
        {
            conn.On<object>("OnVitalReadingCreated", e => OnVitalEvent?.Invoke("Created", e));
        });

        await ConnectHubAsync("medicalalerts", "p7-familyhealth", "/hubs/medicalalerts", conn =>
        {
            conn.On<object>("OnMedicalAlertCreated", e => OnAlertEvent?.Invoke("Created", e));
            conn.On<object>("OnMedicalAlertUpdated", e => OnAlertEvent?.Invoke("Updated", e));
        });
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

    public Dictionary<string, HubConnectionState> GetAllConnectionStates()
    {
        return _connections.ToDictionary(c => c.Key, c => c.Value.State);
    }

    /// <summary>
    /// Join a group on a specific hub.
    /// </summary>
    public async Task JoinGroupAsync(string hubName, string entityType, string groupId)
    {
        if (!_connections.TryGetValue(hubName, out var conn)) return;
        if (conn.State != HubConnectionState.Connected) return;

        await conn.InvokeAsync($"Join{entityType}Group", groupId);
        _logger.LogInformation("Joined group {GroupId} on {Hub}", groupId, hubName);
    }

    /// <summary>
    /// Leave a group on a specific hub.
    /// </summary>
    public async Task LeaveGroupAsync(string hubName, string entityType, string groupId)
    {
        if (!_connections.TryGetValue(hubName, out var conn)) return;
        if (conn.State != HubConnectionState.Connected) return;

        await conn.InvokeAsync($"Leave{entityType}Group", groupId);
    }

    // === Internal Connection Management ===

    private async Task ConnectHubAsync(
        string name, string serviceName, string hubPath,
        Action<HubConnection> registerHandlers)
    {
        if (_connections.ContainsKey(name)) return;

        var baseUrl = ResolveServiceUrl(serviceName);
        var hubUrl = $"{baseUrl}{hubPath}";

        var conn = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new DashboardRetryPolicy(_logger, name))
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

        registerHandlers(conn);
        _connections[name] = conn;

        try
        {
            await conn.StartAsync();
            _logger.LogInformation("Connected to {Hub} at {Url}", name, hubUrl);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to {Hub} at {Url}", name, hubUrl);
            OnConnectionStateChanged?.Invoke(name, HubConnectionState.Disconnected);
        }
    }

    private string ResolveServiceUrl(string serviceName)
    {
        // Try Aspire service discovery
        var httpUrl = _config[$"services:{serviceName}:http:0"];
        if (!string.IsNullOrEmpty(httpUrl))
            return httpUrl.TrimEnd('/');

        var httpsUrl = _config[$"services:{serviceName}:https:0"];
        if (!string.IsNullOrEmpty(httpsUrl))
            return httpsUrl.TrimEnd('/');

        // Fallback to localhost convention
        var portMap = new Dictionary<string, int>
        {
            ["p2-voiceemergency"] = 5002,
            ["p6-firstresponder"] = 5006,
            ["p7-familyhealth"] = 5007
        };

        return portMap.TryGetValue(serviceName, out var port)
            ? $"http://localhost:{port}"
            : $"http://localhost:5000";
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
/// Exponential backoff retry policy for Dashboard SignalR connections.
/// Delays: 0s, 2s, 4s, 8s, 16s, 30s, 30s... (max 10 retries)
/// </summary>
public class DashboardRetryPolicy : IRetryPolicy
{
    private readonly ILogger _logger;
    private readonly string _hubName;
    private const int MaxRetries = 10;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    public DashboardRetryPolicy(ILogger logger, string hubName)
    {
        _logger = logger;
        _hubName = hubName;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= MaxRetries)
        {
            _logger.LogWarning("SignalR {Hub}: max retries ({MaxRetries}) reached", _hubName, MaxRetries);
            return null;
        }

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryContext.PreviousRetryCount));
        if (delay > MaxDelay) delay = MaxDelay;

        _logger.LogInformation("SignalR {Hub}: retry #{Attempt} in {Delay}s",
            _hubName, retryContext.PreviousRetryCount + 1, delay.TotalSeconds);

        return delay;
    }
}
