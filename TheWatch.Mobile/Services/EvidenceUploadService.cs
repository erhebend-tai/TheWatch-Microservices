using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Uploads evidence files to the server with chunked upload for large files,
/// retry logic, and per-file progress tracking.
/// Pauses when offline or battery < 10%.
/// </summary>
public class EvidenceUploadService : IDisposable
{
    private readonly EvidenceMetadataService _metadata;
    private readonly ChainOfCustodyService _custody;
    private readonly ConnectivityMonitorService _connectivity;
    private readonly BatteryMonitorService _battery;
    private readonly HttpClient _http;
    private readonly ILogger<EvidenceUploadService> _logger;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private const long ChunkSize = 10 * 1024 * 1024; // 10 MB

    public event Action<Guid, double>? OnUploadProgressChanged;
    public event Action<Guid, bool>? OnUploadCompleted;

    public EvidenceUploadService(
        EvidenceMetadataService metadata,
        ChainOfCustodyService custody,
        ConnectivityMonitorService connectivity,
        BatteryMonitorService battery,
        HttpClient http,
        ILogger<EvidenceUploadService> logger)
    {
        _metadata = metadata;
        _custody = custody;
        _connectivity = connectivity;
        _battery = battery;
        _http = http;
        _logger = logger;
    }

    /// <summary>Start the upload worker that processes queued evidence.</summary>
    public void StartWorker()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = UploadLoopAsync(_cts.Token);
    }

    public void StopWorker()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>Upload a single evidence record immediately.</summary>
    public async Task<bool> UploadAsync(EvidenceRecord record, CancellationToken ct = default)
    {
        if (!_connectivity.IsOnline)
        {
            _logger.LogDebug("Offline — queuing evidence {Id} for later upload", record.Id);
            _metadata.UpdateStatus(record.Id, EvidenceStatus.Queued);
            return false;
        }

        try
        {
            _metadata.UpdateStatus(record.Id, EvidenceStatus.Uploading);
            var fileInfo = new FileInfo(record.FilePath);

            if (fileInfo.Length > ChunkSize)
            {
                await UploadChunkedAsync(record, fileInfo, ct);
            }
            else
            {
                await UploadSingleAsync(record, ct);
            }

            _metadata.UpdateStatus(record.Id, EvidenceStatus.Uploaded);
            OnUploadCompleted?.Invoke(record.Id, true);
            _logger.LogInformation("Evidence uploaded: {Id} ({Size} bytes)", record.Id, fileInfo.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence upload failed: {Id}", record.Id);
            _metadata.UpdateStatus(record.Id, EvidenceStatus.Failed);
            OnUploadCompleted?.Invoke(record.Id, false);
            return false;
        }
    }

    private async Task UploadSingleAsync(EvidenceRecord record, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(record.FilePath);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(record.MimeType);
        content.Add(streamContent, "file", Path.GetFileName(record.FilePath));
        content.Add(new StringContent(record.FileHash), "hash");
        content.Add(new StringContent(record.IncidentId?.ToString() ?? ""), "incidentId");
        content.Add(new StringContent(_custody.ExportChainJson()), "custodyChain");

        var response = await _http.PostAsync("api/evidence/upload", content, ct);
        response.EnsureSuccessStatusCode();

        record.UploadProgress = 1.0;
        OnUploadProgressChanged?.Invoke(record.Id, 1.0);
    }

    private async Task UploadChunkedAsync(EvidenceRecord record, FileInfo fileInfo, CancellationToken ct)
    {
        var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / ChunkSize);

        await using var stream = File.OpenRead(record.FilePath);
        var buffer = new byte[ChunkSize];

        for (var chunk = 0; chunk < totalChunks; chunk++)
        {
            ct.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(buffer, ct);
            using var content = new MultipartFormDataContent();
            var chunkContent = new ByteArrayContent(buffer, 0, bytesRead);
            chunkContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(chunkContent, "chunk", Path.GetFileName(record.FilePath));
            content.Add(new StringContent(chunk.ToString()), "chunkIndex");
            content.Add(new StringContent(totalChunks.ToString()), "totalChunks");
            content.Add(new StringContent(record.Id.ToString()), "evidenceId");

            if (chunk == totalChunks - 1)
            {
                content.Add(new StringContent(record.FileHash), "hash");
                content.Add(new StringContent(_custody.ExportChainJson()), "custodyChain");
            }

            var response = await _http.PostAsync("api/evidence/upload/chunk", content, ct);
            response.EnsureSuccessStatusCode();

            var progress = (double)(chunk + 1) / totalChunks;
            record.UploadProgress = progress;
            OnUploadProgressChanged?.Invoke(record.Id, progress);
        }
    }

    private async Task UploadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                // Don't upload if offline or battery critically low
                if (!_connectivity.IsOnline || _battery.CurrentBatteryLevel < 0.10)
                {
                    await Task.Delay(10000, ct);
                    continue;
                }

                var pending = _metadata.GetPendingUpload();
                foreach (var record in pending)
                {
                    if (ct.IsCancellationRequested) break;
                    await UploadAsync(record, ct);
                }

                await Task.Delay(5000, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Evidence upload loop error");
                await Task.Delay(30000, ct);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
