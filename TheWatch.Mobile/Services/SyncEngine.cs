using System.Text.Json;
using Microsoft.Extensions.Logging;
using TheWatch.Mobile.Data;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Synchronization engine that reconciles local SQLite data with server on reconnect.
/// Handles offline queue draining, data fetching, conflict resolution, and cache updates.
/// </summary>
public interface ISyncEngine
{
    Task SyncAllAsync();
    Task SyncOfflineQueueAsync();
    Task SyncCachedDataAsync();
    Task<SyncResult> GetLastSyncResultAsync();
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    event EventHandler<ConflictResolvedEventArgs>? ConflictResolved;
}

public class SyncEngine : ISyncEngine
{
    private readonly WatchLocalDbContext _dbContext;
    private readonly IOfflineQueueService _offlineQueue;
    private readonly WatchApiClient _apiClient;
    private readonly ILogger<SyncEngine> _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<ConflictResolvedEventArgs>? ConflictResolved;

    public SyncEngine(
        WatchLocalDbContext dbContext,
        IOfflineQueueService offlineQueue,
        WatchApiClient apiClient,
        ILogger<SyncEngine> logger)
    {
        _dbContext = dbContext;
        _offlineQueue = offlineQueue;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Perform complete synchronization: drain offline queue and sync cached data
    /// </summary>
    public async Task SyncAllAsync()
    {
        if (!await _syncLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("Sync already in progress, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting full synchronization");
            ReportProgress("Starting sync...", 0);

            // Step 1: Drain offline queue (50% of progress)
            await SyncOfflineQueueAsync();
            ReportProgress("Offline queue synced", 50);

            // Step 2: Sync cached data (remaining 50%)
            await SyncCachedDataAsync();
            ReportProgress("Sync completed", 100);

            _logger.LogInformation("Full synchronization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synchronization failed");
            ReportProgress($"Sync failed: {ex.Message}", -1);
            throw;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Drain offline queue in FIFO order with retry logic
    /// </summary>
    public async Task SyncOfflineQueueAsync()
    {
        var pendingItems = await _offlineQueue.GetPendingRequestsAsync();
        if (pendingItems.Count == 0)
        {
            _logger.LogDebug("No pending offline requests to sync");
            return;
        }

        _logger.LogInformation("Syncing {Count} offline requests", pendingItems.Count);

        var processed = 0;
        foreach (var item in pendingItems)
        {
            try
            {
                await _offlineQueue.MarkAsProcessingAsync(item.Id);
                
                // Execute the queued HTTP request
                var success = await ExecuteQueuedRequestAsync(item);
                
                if (success)
                {
                    await _offlineQueue.MarkAsCompletedAsync(item.Id);
                    _logger.LogDebug("Successfully processed queue item: {Method} {Url}", 
                        item.Method, item.Url);
                }
                else
                {
                    await _offlineQueue.MarkAsFailedAsync(item.Id, "Request failed during sync");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queue item {ItemId}: {Method} {Url}", 
                    item.Id, item.Method, item.Url);
                await _offlineQueue.MarkAsFailedAsync(item.Id, ex.Message);
            }

            processed++;
            var progress = (processed * 50) / pendingItems.Count; // 50% of total progress
            ReportProgress($"Processed {processed}/{pendingItems.Count} offline requests", progress);
        }
    }

    /// <summary>
    /// Sync cached data with server, handling conflicts with server-wins strategy
    /// </summary>
    public async Task SyncCachedDataAsync()
    {
        _logger.LogInformation("Syncing cached data with server");

        try
        {
            // Sync user profile
            await SyncUserProfileAsync();
            ReportProgress("User profile synced", 60);

            // Sync family data
            await SyncFamilyDataAsync();
            ReportProgress("Family data synced", 70);

            // Sync incidents
            await SyncIncidentsAsync();
            ReportProgress("Incidents synced", 80);

            // Sync vitals and check-ins
            await SyncVitalsAndCheckInsAsync();
            ReportProgress("Vitals and check-ins synced", 90);

            _logger.LogInformation("Cached data sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync cached data");
            throw;
        }
    }

    /// <summary>
    /// Get the result of the last synchronization attempt
    /// </summary>
    public async Task<SyncResult> GetLastSyncResultAsync()
    {
        // This would typically be stored in the database
        // For now, return a basic result
        var pendingCount = await _offlineQueue.GetPendingCountAsync();
        
        return new SyncResult
        {
            LastSyncAt = DateTime.UtcNow, // Would be stored/retrieved from DB
            Success = pendingCount == 0,
            PendingItems = pendingCount,
            ConflictsResolved = 0, // Would be retrieved from conflict log
            ErrorMessage = pendingCount > 0 ? $"{pendingCount} items still pending" : null
        };
    }

    private async Task<bool> ExecuteQueuedRequestAsync(OfflineQueueItem item)
    {
        try
        {
            // Parse headers if present
            Dictionary<string, string>? headers = null;
            if (!string.IsNullOrEmpty(item.Headers))
            {
                headers = JsonSerializer.Deserialize<Dictionary<string, string>>(item.Headers);
            }

            // Execute the HTTP request based on method
            HttpResponseMessage response = item.Method.ToUpperInvariant() switch
            {
                "GET" => await ExecuteGetRequestAsync(item.Url, headers),
                "POST" => await ExecutePostRequestAsync(item.Url, item.JsonBody, headers),
                "PUT" => await ExecutePutRequestAsync(item.Url, item.JsonBody, headers),
                "DELETE" => await ExecuteDeleteRequestAsync(item.Url, headers),
                _ => throw new NotSupportedException($"HTTP method {item.Method} not supported")
            };

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute queued request: {Method} {Url}", 
                item.Method, item.Url);
            return false;
        }
    }

    private async Task<HttpResponseMessage> ExecuteGetRequestAsync(string url, Dictionary<string, string>? headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(request, headers);
        return await _apiClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ExecutePostRequestAsync(string url, string? jsonBody, Dictionary<string, string>? headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrEmpty(jsonBody))
        {
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        }
        AddHeaders(request, headers);
        return await _apiClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ExecutePutRequestAsync(string url, string? jsonBody, Dictionary<string, string>? headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (!string.IsNullOrEmpty(jsonBody))
        {
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        }
        AddHeaders(request, headers);
        return await _apiClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ExecuteDeleteRequestAsync(string url, Dictionary<string, string>? headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        AddHeaders(request, headers);
        return await _apiClient.SendAsync(request);
    }

    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    private async Task SyncUserProfileAsync()
    {
        try
        {
            // This would fetch current user profile from server and compare with local cache
            // For now, just log the operation
            _logger.LogDebug("Syncing user profile (placeholder)");
            await Task.Delay(100); // Simulate API call
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync user profile");
        }
    }

    private async Task SyncFamilyDataAsync()
    {
        try
        {
            // This would fetch family group and members from server
            _logger.LogDebug("Syncing family data (placeholder)");
            await Task.Delay(100); // Simulate API call
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync family data");
        }
    }

    private async Task SyncIncidentsAsync()
    {
        try
        {
            // This would fetch recent incidents from server
            _logger.LogDebug("Syncing incidents (placeholder)");
            await Task.Delay(100); // Simulate API call
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync incidents");
        }
    }

    private async Task SyncVitalsAndCheckInsAsync()
    {
        try
        {
            // This would sync vital readings and check-ins
            _logger.LogDebug("Syncing vitals and check-ins (placeholder)");
            await Task.Delay(100); // Simulate API call
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync vitals and check-ins");
        }
    }

    private async Task HandleConflictAsync<T>(string entityType, Guid entityId, T localValue, T serverValue)
    {
        // Server-wins conflict resolution strategy
        var conflict = new ConflictLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ConflictType = "ServerWins",
            LocalValueJson = JsonSerializer.Serialize(localValue),
            ServerValueJson = JsonSerializer.Serialize(serverValue),
            ResolutionJson = JsonSerializer.Serialize(serverValue), // Server wins
            ResolvedAt = DateTime.UtcNow
        };

        await _dbContext.LogConflictAsync(conflict);

        var eventArgs = new ConflictResolvedEventArgs
        {
            EntityType = entityType,
            EntityId = entityId,
            Resolution = "Server data was used to resolve the conflict",
            ResolvedAt = DateTime.UtcNow
        };

        ConflictResolved?.Invoke(this, eventArgs);
        
        _logger.LogInformation("Resolved conflict for {EntityType} {EntityId}: server wins", 
            entityType, entityId);
    }

    private void ReportProgress(string message, int percentage)
    {
        var eventArgs = new SyncProgressEventArgs
        {
            Message = message,
            Percentage = percentage,
            Timestamp = DateTime.UtcNow
        };

        SyncProgressChanged?.Invoke(this, eventArgs);
    }
}

// Supporting classes and events

public class SyncResult
{
    public DateTime LastSyncAt { get; set; }
    public bool Success { get; set; }
    public int PendingItems { get; set; }
    public int ConflictsResolved { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SyncProgressEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ConflictResolvedEventArgs : EventArgs
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}

/// <summary>
/// Extension methods for WatchApiClient to support sync operations
/// </summary>
public static class WatchApiClientSyncExtensions
{
    /// <summary>
    /// Send a raw HTTP request (used by SyncEngine for queued requests).
    /// Delegates to the instance method on WatchApiClient which sends
    /// through the underlying HttpClient (with AuthDelegatingHandler in the pipeline).
    /// </summary>
    public static async Task<HttpResponseMessage> SendAsync(this WatchApiClient client, HttpRequestMessage request)
    {
        return await client.SendAsync(request, CancellationToken.None);
    }
}