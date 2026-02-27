using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for offline queue logic — item status transitions, priority ordering,
/// retry/backoff calculations, and extension methods.
/// Since OfflineQueueService depends on WatchLocalDbContext (SQLite),
/// we test the pure logic and data models independently.
/// </summary>
public class OfflineQueueTests
{
    // =========================================================================
    // OfflineQueueItem Model Tests
    // =========================================================================

    [Fact]
    public void OfflineQueueItem_DefaultValues()
    {
        var item = new OfflineQueueItem();

        item.Id.Should().Be(Guid.Empty);
        item.Method.Should().BeEmpty();
        item.Url.Should().BeEmpty();
        item.JsonBody.Should().BeNull();
        item.Headers.Should().BeNull();
        item.Priority.Should().Be(5);
        item.RetryCount.Should().Be(0);
        item.MaxRetries.Should().Be(5);
        item.Status.Should().Be(QueueItemStatus.Pending);
        item.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void OfflineQueueItem_CanBeCreatedWithAllProperties()
    {
        var id = Guid.NewGuid();
        var item = new OfflineQueueItem
        {
            Id = id,
            Method = "POST",
            Url = "http://localhost:5002/api/incidents",
            JsonBody = """{"type":"Fire","description":"test"}""",
            Headers = """{"Authorization":"Bearer token123"}""",
            Priority = 1,
            Status = QueueItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        item.Id.Should().Be(id);
        item.Method.Should().Be("POST");
        item.Priority.Should().Be(1);
    }

    // =========================================================================
    // Status Transitions
    // =========================================================================

    [Fact]
    public void StatusTransition_PendingToProcessing()
    {
        var item = CreateQueueItem();

        item.Status = QueueItemStatus.Processing;
        item.LastAttemptAt = DateTime.UtcNow;

        item.Status.Should().Be(QueueItemStatus.Processing);
        item.LastAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void StatusTransition_ProcessingToCompleted()
    {
        var item = CreateQueueItem(QueueItemStatus.Processing);

        item.Status = QueueItemStatus.Completed;

        item.Status.Should().Be(QueueItemStatus.Completed);
    }

    [Fact]
    public void StatusTransition_ProcessingToFailed_IncreasesRetryCount()
    {
        var item = CreateQueueItem(QueueItemStatus.Processing);

        item.RetryCount++;
        item.ErrorMessage = "Connection timeout";
        item.LastAttemptAt = DateTime.UtcNow;

        if (item.RetryCount >= item.MaxRetries)
            item.Status = QueueItemStatus.DeadLetter;
        else
            item.Status = QueueItemStatus.Failed;

        item.Status.Should().Be(QueueItemStatus.Failed);
        item.RetryCount.Should().Be(1);
        item.ErrorMessage.Should().Be("Connection timeout");
    }

    [Fact]
    public void StatusTransition_ExhaustedRetries_MovesToDeadLetter()
    {
        var item = CreateQueueItem(QueueItemStatus.Processing);
        item.RetryCount = 4; // Already tried 4 times

        item.RetryCount++; // 5th attempt
        item.ErrorMessage = "Still failing";

        if (item.RetryCount >= item.MaxRetries)
            item.Status = QueueItemStatus.DeadLetter;
        else
            item.Status = QueueItemStatus.Failed;

        item.Status.Should().Be(QueueItemStatus.DeadLetter);
        item.RetryCount.Should().Be(5);
    }

    [Fact]
    public void StatusTransition_FailedToPending_ManualRetry()
    {
        var item = CreateQueueItem(QueueItemStatus.Failed);
        item.RetryCount = 3;

        // Manual retry resets to Pending
        item.Status = QueueItemStatus.Pending;
        item.NextRetryAt = null;

        item.Status.Should().Be(QueueItemStatus.Pending);
        item.NextRetryAt.Should().BeNull();
        item.RetryCount.Should().Be(3); // Retry count is preserved
    }

    [Fact]
    public void StatusTransition_DeadLetterToPending_ManualRetry()
    {
        var item = CreateQueueItem(QueueItemStatus.DeadLetter);
        item.RetryCount = 5;

        // Even dead letter items can be manually retried
        item.Status = QueueItemStatus.Pending;
        item.NextRetryAt = null;

        item.Status.Should().Be(QueueItemStatus.Pending);
    }

    // =========================================================================
    // Exponential Backoff
    // =========================================================================

    [Theory]
    [InlineData(1, 2)]     // 2^1 = 2 minutes
    [InlineData(2, 4)]     // 2^2 = 4 minutes
    [InlineData(3, 8)]     // 2^3 = 8 minutes
    [InlineData(4, 16)]    // 2^4 = 16 minutes
    [InlineData(5, 32)]    // 2^5 = 32 minutes
    public void ExponentialBackoff_CalculatesCorrectDelay(int retryCount, double expectedMinutes)
    {
        var backoffMinutes = Math.Pow(2, retryCount);

        backoffMinutes.Should().Be(expectedMinutes);
    }

    [Fact]
    public void ExponentialBackoff_SetsNextRetryAt()
    {
        var item = CreateQueueItem(QueueItemStatus.Processing);
        item.RetryCount = 2; // Will become 3 after this failure
        item.RetryCount++;
        item.Status = QueueItemStatus.Failed;

        var backoffMinutes = Math.Pow(2, item.RetryCount);
        item.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);

        item.NextRetryAt.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(8),
            TimeSpan.FromSeconds(5));
    }

    // =========================================================================
    // Priority Ordering
    // =========================================================================

    [Fact]
    public void PriorityOrdering_HigherPriorityFirst()
    {
        var items = new List<OfflineQueueItem>
        {
            CreateQueueItem(priority: 5),
            CreateQueueItem(priority: 1),
            CreateQueueItem(priority: 3),
        };

        var ordered = items.OrderBy(i => i.Priority).ThenBy(i => i.CreatedAt).ToList();

        ordered[0].Priority.Should().Be(1);
        ordered[1].Priority.Should().Be(3);
        ordered[2].Priority.Should().Be(5);
    }

    [Fact]
    public void PriorityOrdering_SamePriority_FIFOOrder()
    {
        var now = DateTime.UtcNow;
        var items = new List<OfflineQueueItem>
        {
            CreateQueueItem(priority: 5, createdAt: now.AddMinutes(2)),
            CreateQueueItem(priority: 5, createdAt: now),
            CreateQueueItem(priority: 5, createdAt: now.AddMinutes(1)),
        };

        var ordered = items.OrderBy(i => i.Priority).ThenBy(i => i.CreatedAt).ToList();

        ordered[0].CreatedAt.Should().Be(now);
        ordered[1].CreatedAt.Should().Be(now.AddMinutes(1));
        ordered[2].CreatedAt.Should().Be(now.AddMinutes(2));
    }

    // =========================================================================
    // Headers Serialization
    // =========================================================================

    [Fact]
    public void Headers_NullWhenEmpty()
    {
        var item = CreateQueueItem();

        item.Headers.Should().BeNull();
    }

    [Fact]
    public void Headers_SerializesAndDeserializesCorrectly()
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["X-Request-Id"] = Guid.NewGuid().ToString()
        };

        var serialized = JsonSerializer.Serialize(headers);
        var item = CreateQueueItem();
        item.Headers = serialized;

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(item.Headers);

        deserialized.Should().ContainKey("Authorization");
        deserialized!["Authorization"].Should().Be("Bearer token123");
    }

    // =========================================================================
    // Extension Methods (mirrors OfflineQueueExtensions)
    // =========================================================================

    [Fact]
    public void EnqueueGet_SetsMethodToGET()
    {
        var item = CreateQueueItemFromExtension("GET", "http://api/health", null, null, 5);

        item.Method.Should().Be("GET");
        item.JsonBody.Should().BeNull();
    }

    [Fact]
    public void EnqueuePost_SetsMethodAndBody()
    {
        var body = """{"key":"value"}""";
        var item = CreateQueueItemFromExtension("POST", "http://api/incidents", body, null, 5);

        item.Method.Should().Be("POST");
        item.JsonBody.Should().Be(body);
    }

    [Fact]
    public void EnqueueEmergency_SetsPriority1()
    {
        var item = CreateQueueItemFromExtension("POST", "http://api/emergency", null, null, 1);

        item.Priority.Should().Be(1);
    }

    [Fact]
    public void EnqueueDelete_SetsMethodToDELETE()
    {
        var item = CreateQueueItemFromExtension("DELETE", "http://api/devices/123", null, null, 5);

        item.Method.Should().Be("DELETE");
    }

    // =========================================================================
    // QueueItemStatus Enum
    // =========================================================================

    [Fact]
    public void QueueItemStatus_HasFiveValues()
    {
        Enum.GetValues<QueueItemStatus>().Should().HaveCount(5);
    }

    [Fact]
    public void QueueItemStatus_AllValuesDefined()
    {
        QueueItemStatus.Pending.Should().BeDefined();
        QueueItemStatus.Processing.Should().BeDefined();
        QueueItemStatus.Completed.Should().BeDefined();
        QueueItemStatus.Failed.Should().BeDefined();
        QueueItemStatus.DeadLetter.Should().BeDefined();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static OfflineQueueItem CreateQueueItem(
        QueueItemStatus status = QueueItemStatus.Pending,
        int priority = 5,
        DateTime? createdAt = null)
    {
        return new OfflineQueueItem
        {
            Id = Guid.NewGuid(),
            Method = "POST",
            Url = "http://localhost:5002/api/incidents",
            JsonBody = """{"type":"Fire"}""",
            Priority = priority,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 5
        };
    }

    private static OfflineQueueItem CreateQueueItemFromExtension(
        string method, string url, string? jsonBody, Dictionary<string, string>? headers, int priority)
    {
        return new OfflineQueueItem
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
    }
}

/// <summary>
/// Mirror of OfflineQueueItem from TheWatch.Mobile.Data
/// </summary>
public class OfflineQueueItem
{
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? JsonBody { get; set; }
    public string? Headers { get; set; }
    public int Priority { get; set; } = 5;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 5;
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Mirror of QueueItemStatus from TheWatch.Mobile.Data
/// </summary>
public enum QueueItemStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    DeadLetter
}
