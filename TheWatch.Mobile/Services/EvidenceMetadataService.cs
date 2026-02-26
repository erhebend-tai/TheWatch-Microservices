using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Stamps each evidence capture with metadata: GPS, timestamp, device info, user ID.
/// Creates EvidenceRecord objects for local storage and server upload.
/// </summary>
public class EvidenceMetadataService
{
    private readonly WatchAuthService _auth;
    private readonly EmergencyLocationService _location;
    private readonly ChainOfCustodyService _custody;
    private readonly ILogger<EvidenceMetadataService> _logger;
    private readonly List<EvidenceRecord> _records = [];

    public IReadOnlyList<EvidenceRecord> Records => _records.AsReadOnly();

    public EvidenceMetadataService(
        WatchAuthService auth,
        EmergencyLocationService location,
        ChainOfCustodyService custody,
        ILogger<EvidenceMetadataService> logger)
    {
        _auth = auth;
        _location = location;
        _custody = custody;
        _logger = logger;
    }

    /// <summary>
    /// Create an evidence record for a captured media file with full metadata.
    /// </summary>
    public async Task<EvidenceRecord> CreateRecordAsync(CapturedMedia media, Guid? incidentId = null)
    {
        var loc = _location.LastLocation;
        var custodyRecord = await _custody.CreateRecordAsync(media.FilePath);

        var record = new EvidenceRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            FilePath = media.FilePath,
            MimeType = media.MimeType,
            FileSize = media.FileSize,
            MediaType = media.MediaType,
            CapturedAt = media.CapturedAt,
            Latitude = loc?.Latitude ?? 0,
            Longitude = loc?.Longitude ?? 0,
            Accuracy = loc?.Accuracy ?? 0,
            DeviceModel = DeviceInfo.Current.Model,
            DeviceManufacturer = DeviceInfo.Current.Manufacturer,
            DevicePlatform = DeviceInfo.Current.Platform.ToString(),
            UserId = _auth.CurrentUser?.Id ?? Guid.Empty,
            FileHash = custodyRecord.FileHash,
            CustodyRecordId = custodyRecord.Id,
            Status = EvidenceStatus.Captured
        };

        _records.Add(record);
        _logger.LogInformation("Evidence record created: {Id} ({Type}, {Size} bytes)",
            record.Id, record.MediaType, record.FileSize);

        return record;
    }

    public List<EvidenceRecord> GetByIncident(Guid incidentId)
        => _records.Where(r => r.IncidentId == incidentId).ToList();

    public List<EvidenceRecord> GetPendingUpload()
        => _records.Where(r => r.Status is EvidenceStatus.Captured or EvidenceStatus.Queued).ToList();

    public void UpdateStatus(Guid recordId, EvidenceStatus status)
    {
        var record = _records.FirstOrDefault(r => r.Id == recordId);
        if (record is not null)
        {
            record.Status = status;
        }
    }
}

public class EvidenceRecord
{
    public Guid Id { get; set; }
    public Guid? IncidentId { get; set; }
    public string FilePath { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public MediaType MediaType { get; set; }
    public DateTime CapturedAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public string DeviceModel { get; set; } = "";
    public string DeviceManufacturer { get; set; } = "";
    public string DevicePlatform { get; set; } = "";
    public Guid UserId { get; set; }
    public string FileHash { get; set; } = "";
    public Guid CustodyRecordId { get; set; }
    public EvidenceStatus Status { get; set; }
    public double UploadProgress { get; set; }
    public bool IsFlagged { get; set; }
    public double? ModerationScore { get; set; }
}

public enum EvidenceStatus
{
    Captured,
    Queued,
    Uploading,
    Uploaded,
    Verified,
    Failed
}
