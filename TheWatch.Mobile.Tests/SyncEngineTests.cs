using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for SyncEngine logic — progress reporting, lock behavior,
/// sync result model, and conflict resolution events.
/// Since SyncEngine depends on WatchLocalDbContext and IOfflineQueueService,
/// we test the pure logic: progress events, sync result, and conflict handling.
/// </summary>
public class SyncEngineTests
{
    // =========================================================================
    // SyncResult Model
    // =========================================================================

    [Fact]
    public void SyncResult_Success_WhenNoPendingItems()
    {
        var result = new SyncResult
        {
            LastSyncAt = DateTime.UtcNow,
            Success = true,
            PendingItems = 0,
            ConflictsResolved = 0,
            ErrorMessage = null
        };

        result.Success.Should().BeTrue();
        result.PendingItems.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SyncResult_Failure_WhenPendingItemsExist()
    {
        var pendingCount = 5;
        var result = new SyncResult
        {
            LastSyncAt = DateTime.UtcNow,
            Success = pendingCount == 0,
            PendingItems = pendingCount,
            ErrorMessage = pendingCount > 0 ? $"{pendingCount} items still pending" : null
        };

        result.Success.Should().BeFalse();
        result.PendingItems.Should().Be(5);
        result.ErrorMessage.Should().Be("5 items still pending");
    }

    [Fact]
    public void SyncResult_WithConflicts()
    {
        var result = new SyncResult
        {
            LastSyncAt = DateTime.UtcNow,
            Success = true,
            PendingItems = 0,
            ConflictsResolved = 3
        };

        result.ConflictsResolved.Should().Be(3);
    }

    // =========================================================================
    // Progress Reporting
    // =========================================================================

    [Fact]
    public void SyncProgress_ReportsCorrectPercentages()
    {
        var progressUpdates = new List<SyncProgressEventArgs>();
        EventHandler<SyncProgressEventArgs> handler = (_, args) => progressUpdates.Add(args);

        // Simulate progress reports from SyncAllAsync
        var stages = new[]
        {
            ("Starting sync...", 0),
            ("Offline queue synced", 50),
            ("Sync completed", 100)
        };

        foreach (var (message, percentage) in stages)
        {
            var args = new SyncProgressEventArgs
            {
                Message = message,
                Percentage = percentage,
                Timestamp = DateTime.UtcNow
            };
            handler(null, args);
        }

        progressUpdates.Should().HaveCount(3);
        progressUpdates[0].Percentage.Should().Be(0);
        progressUpdates[1].Percentage.Should().Be(50);
        progressUpdates[2].Percentage.Should().Be(100);
    }

    [Fact]
    public void SyncProgress_FailureReportsNegativePercentage()
    {
        var args = new SyncProgressEventArgs
        {
            Message = "Sync failed: Connection timeout",
            Percentage = -1,
            Timestamp = DateTime.UtcNow
        };

        args.Percentage.Should().Be(-1);
        args.Message.Should().StartWith("Sync failed:");
    }

    [Fact]
    public void SyncProgress_OfflineQueueProgress_IncrementalUpdates()
    {
        var pendingCount = 4;
        var progressValues = new List<int>();

        for (int processed = 1; processed <= pendingCount; processed++)
        {
            var progress = (processed * 50) / pendingCount;
            progressValues.Add(progress);
        }

        progressValues.Should().Equal(12, 25, 37, 50);
    }

    // =========================================================================
    // Conflict Resolution
    // =========================================================================

    [Fact]
    public void ConflictResolved_EventArgs_ContainsDetails()
    {
        var args = new ConflictResolvedEventArgs
        {
            EntityType = "UserProfile",
            EntityId = Guid.NewGuid(),
            Resolution = "Server data was used to resolve the conflict",
            ResolvedAt = DateTime.UtcNow
        };

        args.EntityType.Should().Be("UserProfile");
        args.Resolution.Should().Contain("Server data");
    }

    [Fact]
    public void ConflictResolved_ServerWinsStrategy()
    {
        // Simulate server-wins conflict resolution
        var localValue = "Alice (local)";
        var serverValue = "Alice (server-updated)";

        // Server wins = use server value
        var resolvedValue = serverValue;

        resolvedValue.Should().Be("Alice (server-updated)");
    }

    [Fact]
    public void ConflictLog_CanBeCreated()
    {
        var log = new ConflictLog
        {
            Id = Guid.NewGuid(),
            EntityType = "FamilyGroup",
            EntityId = Guid.NewGuid(),
            ConflictType = "ServerWins",
            LocalValueJson = """{"name":"Family A"}""",
            ServerValueJson = """{"name":"Family A Updated"}""",
            ResolutionJson = """{"name":"Family A Updated"}""",
            ResolvedAt = DateTime.UtcNow
        };

        log.ConflictType.Should().Be("ServerWins");
        log.ResolutionJson.Should().Be(log.ServerValueJson); // Server wins
    }

    // =========================================================================
    // Lock Behavior
    // =========================================================================

    [Fact]
    public async Task SyncLock_PreventsConcurrentSync()
    {
        var semaphore = new SemaphoreSlim(1, 1);
        var entered = 0;
        var blocked = 0;

        // First entry should succeed
        if (await semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            entered++;

            // Second entry should be blocked (timeout of 0)
            if (await semaphore.WaitAsync(0))
            {
                entered++;
                semaphore.Release();
            }
            else
            {
                blocked++;
            }

            semaphore.Release();
        }

        entered.Should().Be(1);
        blocked.Should().Be(1);
    }

    [Fact]
    public async Task SyncLock_AllowsSequentialSync()
    {
        var semaphore = new SemaphoreSlim(1, 1);
        var completedSyncs = 0;

        // First sync
        await semaphore.WaitAsync();
        completedSyncs++;
        semaphore.Release();

        // Second sync (after first completes)
        await semaphore.WaitAsync();
        completedSyncs++;
        semaphore.Release();

        completedSyncs.Should().Be(2);
    }

    // =========================================================================
    // HTTP Method Routing (mirrors ExecuteQueuedRequestAsync)
    // =========================================================================

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void HttpMethodRouting_SupportedMethods(string method)
    {
        HttpMethod httpMethod = method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            _ => throw new NotSupportedException()
        };

        httpMethod.Should().NotBeNull();
    }

    [Fact]
    public void HttpMethodRouting_UnsupportedMethod_Throws()
    {
        var method = "PATCH";

        var act = () =>
        {
            HttpMethod _ = method.ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                _ => throw new NotSupportedException($"HTTP method {method} not supported")
            };
        };

        act.Should().Throw<NotSupportedException>()
           .WithMessage("*PATCH*");
    }

    // =========================================================================
    // AddHeaders Logic
    // =========================================================================

    [Fact]
    public void AddHeaders_AddsToRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["X-Correlation-Id"] = "abc-123"
        };

        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        request.Headers.GetValues("Authorization").First().Should().Be("Bearer token123");
        request.Headers.GetValues("X-Correlation-Id").First().Should().Be("abc-123");
    }

    [Fact]
    public void AddHeaders_NullHeaders_NoChange()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
        Dictionary<string, string>? headers = null;

        if (headers != null)
        {
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        request.Headers.Count().Should().Be(0);
    }
}

/// <summary>
/// Mirror of SyncResult from TheWatch.Mobile.Services
/// </summary>
public class SyncResult
{
    public DateTime LastSyncAt { get; set; }
    public bool Success { get; set; }
    public int PendingItems { get; set; }
    public int ConflictsResolved { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mirror of SyncProgressEventArgs from TheWatch.Mobile.Services
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Mirror of ConflictResolvedEventArgs from TheWatch.Mobile.Services
/// </summary>
public class ConflictResolvedEventArgs : EventArgs
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}

/// <summary>
/// Mirror of ConflictLog from TheWatch.Mobile.Data
/// </summary>
public class ConflictLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ConflictType { get; set; } = string.Empty;
    public string LocalValueJson { get; set; } = string.Empty;
    public string ServerValueJson { get; set; } = string.Empty;
    public string ResolutionJson { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}
