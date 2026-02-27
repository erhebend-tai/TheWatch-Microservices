using System.Text.Json;
using Microsoft.Extensions.Logging;
using TheWatch.Mobile.Data;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Buffers failed API requests when device is offline.
/// Requests are stored in local SQLite via WatchLocalDbContext and drained
/// in FIFO order when connectivity is restored.
/// </summary>
public interface IOfflineQueueService
{
    Task EnqueueAsync(string method, string url, string? jsonBody = null, Dictionary<string, string>? headers = null, int priority = 5);
    Task<List<OfflineQueueItem>> GetPendingRequestsAsync();
    Task MarkAsProcessingAsync(Guid itemId);
    Task MarkAsCompletedAsync(Guid itemId);
    Task MarkAsFailedAsync(Guid itemId, string errorMessage);
    Task RetryFailedAsync(Guid itemId);
    Task<int> GetPendingCountAsync();
    Task ClearCompletedAsync();
}

public class OfflineQueueService : IOfflineQueueService
{
    private readonly WatchLocalDbContext _dbContext;
    private readonly ILogger<OfflineQueueService> _logger;

    public OfflineQueueService(WatchLocalDbContext dbContext, ILogger<OfflineQueueService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a failed API request for later retry when connectivity is restored
    /// </summary>
    public async Task EnqueueAsync(string method, string url, string? jsonBody = null, Dictionary<string, string>? headers = null, int priority = 5)
    {
        var item = new OfflineQueueItem
        {
            Id = Guid.NewGuid(),
            Method = method.ToUpperInvariant(),
            Url = url,
            JsonBody = jsonBody,
            Headers = headers != null ? JsonSerializer.Serialize(headers) : null,
            Priority = priority,
            Status = QueueItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 5
        };

        await _dbContext.EnqueueRequestAsync(item);
        
        _logger.LogInformation("Enqueued offline request: {Method} {Url} (Priority: {Priority})", 
            method, url, priority);
    }

    /// <summary>
    /// Get all pending requests ordered by priority (1 = highest) then by creation time (FIFO)
    /// </summary>
    public async Task<List<OfflineQueueItem>> GetPendingRequestsAsync()
    {
        var items = await _dbContext.GetPendingRequestsAsync();
        return items.OrderBy(i => i.Priority).ThenBy(i => i.CreatedAt).ToList();
    }

    /// <summary>
    /// Mark a queue item as currently being processed
    /// </summary>
    public async Task MarkAsProcessingAsync(Guid itemId)
    {
        var item = await GetQueueItemAsync(itemId);
        if (item != null)
        {
            item.Status = QueueItemStatus.Processing;
            item.LastAttemptAt = DateTime.UtcNow;
            await _dbContext.UpdateQueueItemAsync(item);
            
            _logger.LogDebug("Marked queue item {ItemId} as processing", itemId);
        }
    }

    /// <summary>
    /// Mark a queue item as successfully completed and remove it from the queue
    /// </summary>
    public async Task MarkAsCompletedAsync(Guid itemId)
    {
        var item = await GetQueueItemAsync(itemId);
        if (item != null)
        {
            item.Status = QueueItemStatus.Completed;
            await _dbContext.UpdateQueueItemAsync(item);
            
            _logger.LogInformation("Completed offline request: {Method} {Url}", 
                item.Method, item.Url);
            
            // Remove completed items after a short delay to allow for debugging
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(async _ =>
            {
                await _dbContext.DeleteQueueItemAsync(itemId);
            });
        }
    }

    /// <summary>
    /// Mark a queue item as failed and schedule retry if attempts remain
    /// </summary>
    public async Task MarkAsFailedAsync(Guid itemId, string errorMessage)
    {
        var item = await GetQueueItemAsync(itemId);
        if (item != null)
        {
            item.RetryCount++;
            item.ErrorMessage = errorMessage;
            item.LastAttemptAt = DateTime.UtcNow;

            if (item.RetryCount >= item.MaxRetries)
            {
                item.Status = QueueItemStatus.DeadLetter;
                _logger.LogWarning("Queue item {ItemId} moved to dead letter after {RetryCount} attempts: {Error}", 
                    itemId, item.RetryCount, errorMessage);
            }
            else
            {
                item.Status = QueueItemStatus.Failed;
                // Exponential backoff: 2^retryCount minutes
                var backoffMinutes = Math.Pow(2, item.RetryCount);
                item.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);
                
                _logger.LogWarning("Queue item {ItemId} failed (attempt {RetryCount}/{MaxRetries}), will retry at {NextRetry}: {Error}", 
                    itemId, item.RetryCount, item.MaxRetries, item.NextRetryAt, errorMessage);
            }

            await _dbContext.UpdateQueueItemAsync(item);
        }
    }

    /// <summary>
    /// Retry a failed queue item by resetting its status to pending
    /// </summary>
    public async Task RetryFailedAsync(Guid itemId)
    {
        var item = await GetQueueItemAsync(itemId);
        if (item != null && (item.Status == QueueItemStatus.Failed || item.Status == QueueItemStatus.DeadLetter))
        {
            item.Status = QueueItemStatus.Pending;
            item.NextRetryAt = null;
            await _dbContext.UpdateQueueItemAsync(item);
            
            _logger.LogInformation("Manually retrying queue item {ItemId}: {Method} {Url}", 
                itemId, item.Method, item.Url);
        }
    }

    /// <summary>
    /// Get count of pending requests for UI indicators
    /// </summary>
    public async Task<int> GetPendingCountAsync()
    {
        var items = await _dbContext.GetPendingRequestsAsync();
        return items.Count;
    }

    /// <summary>
    /// Clean up completed queue items older than 1 hour
    /// </summary>
    public async Task ClearCompletedAsync()
    {
        // This would require a custom query in the DbContext
        // For now, completed items are auto-deleted after 5 minutes in MarkAsCompletedAsync
        _logger.LogDebug("Clearing completed queue items");
    }

    private async Task<OfflineQueueItem?> GetQueueItemAsync(Guid itemId)
    {
        var items = await _dbContext.GetPendingRequestsAsync();
        return items.FirstOrDefault(i => i.Id == itemId);
    }
}

/// <summary>
/// Extension methods for common queue operations
/// </summary>
public static class OfflineQueueExtensions
{
    /// <summary>
    /// Enqueue a GET request
    /// </summary>
    public static Task EnqueueGetAsync(this IOfflineQueueService queue, string url, Dictionary<string, string>? headers = null, int priority = 5)
    {
        return queue.EnqueueAsync("GET", url, null, headers, priority);
    }

    /// <summary>
    /// Enqueue a POST request with JSON body
    /// </summary>
    public static Task EnqueuePostAsync(this IOfflineQueueService queue, string url, string jsonBody, Dictionary<string, string>? headers = null, int priority = 5)
    {
        return queue.EnqueueAsync("POST", url, jsonBody, headers, priority);
    }

    /// <summary>
    /// Enqueue a PUT request with JSON body
    /// </summary>
    public static Task EnqueuePutAsync(this IOfflineQueueService queue, string url, string jsonBody, Dictionary<string, string>? headers = null, int priority = 5)
    {
        return queue.EnqueueAsync("PUT", url, jsonBody, headers, priority);
    }

    /// <summary>
    /// Enqueue a DELETE request
    /// </summary>
    public static Task EnqueueDeleteAsync(this IOfflineQueueService queue, string url, Dictionary<string, string>? headers = null, int priority = 5)
    {
        return queue.EnqueueAsync("DELETE", url, null, headers, priority);
    }

    /// <summary>
    /// Enqueue a high-priority emergency request (priority = 1)
    /// </summary>
    public static Task EnqueueEmergencyAsync(this IOfflineQueueService queue, string method, string url, string? jsonBody = null, Dictionary<string, string>? headers = null)
    {
        return queue.EnqueueAsync(method, url, jsonBody, headers, priority: 1);
    }
}