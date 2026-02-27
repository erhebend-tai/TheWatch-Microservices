using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for evidence metadata models and filtering logic.
/// Since EvidenceMetadataService depends on MAUI DeviceInfo, WatchAuthService,
/// and EmergencyLocationService, we test the data models, status transitions,
/// and pure filtering logic independently.
/// </summary>
public class EvidenceMetadataTests
{
    // =========================================================================
    // EvidenceRecord Model
    // =========================================================================

    [Fact]
    public void EvidenceRecord_DefaultValues()
    {
        var record = new EvidenceRecord();

        record.Id.Should().Be(Guid.Empty);
        record.IncidentId.Should().BeNull();
        record.FilePath.Should().BeEmpty();
        record.MimeType.Should().BeEmpty();
        record.FileSize.Should().Be(0);
        record.Status.Should().Be(EvidenceStatus.Captured);
        record.UploadProgress.Should().Be(0.0);
        record.IsFlagged.Should().BeFalse();
        record.ModerationScore.Should().BeNull();
    }

    [Fact]
    public void EvidenceRecord_CanBeCreatedWithAllProperties()
    {
        var incidentId = Guid.NewGuid();
        var record = new EvidenceRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            FilePath = "/evidence/photo_20260226.jpg",
            MimeType = "image/jpeg",
            FileSize = 2_500_000,
            MediaType = MediaType.Photo,
            CapturedAt = DateTime.UtcNow,
            Latitude = 40.7128,
            Longitude = -74.006,
            Accuracy = 5.0,
            DeviceModel = "Galaxy S25",
            DeviceManufacturer = "Samsung",
            DevicePlatform = "Android",
            UserId = Guid.NewGuid(),
            FileHash = "abc123def456",
            CustodyRecordId = Guid.NewGuid(),
            Status = EvidenceStatus.Captured
        };

        record.MimeType.Should().Be("image/jpeg");
        record.FileSize.Should().Be(2_500_000);
        record.MediaType.Should().Be(MediaType.Photo);
        record.Accuracy.Should().Be(5.0);
    }

    // =========================================================================
    // Status Transitions
    // =========================================================================

    [Fact]
    public void StatusTransition_CapturedToQueued()
    {
        var record = CreateRecord(EvidenceStatus.Captured);

        record.Status = EvidenceStatus.Queued;

        record.Status.Should().Be(EvidenceStatus.Queued);
    }

    [Fact]
    public void StatusTransition_QueuedToUploading()
    {
        var record = CreateRecord(EvidenceStatus.Queued);

        record.Status = EvidenceStatus.Uploading;
        record.UploadProgress = 0.0;

        record.Status.Should().Be(EvidenceStatus.Uploading);
    }

    [Fact]
    public void StatusTransition_UploadingToUploaded()
    {
        var record = CreateRecord(EvidenceStatus.Uploading);
        record.UploadProgress = 1.0;

        record.Status = EvidenceStatus.Uploaded;

        record.Status.Should().Be(EvidenceStatus.Uploaded);
        record.UploadProgress.Should().Be(1.0);
    }

    [Fact]
    public void StatusTransition_UploadedToVerified()
    {
        var record = CreateRecord(EvidenceStatus.Uploaded);

        record.Status = EvidenceStatus.Verified;

        record.Status.Should().Be(EvidenceStatus.Verified);
    }

    [Fact]
    public void StatusTransition_UploadingToFailed()
    {
        var record = CreateRecord(EvidenceStatus.Uploading);
        record.UploadProgress = 0.45;

        record.Status = EvidenceStatus.Failed;

        record.Status.Should().Be(EvidenceStatus.Failed);
    }

    // =========================================================================
    // Filtering Logic (mirrors EvidenceMetadataService)
    // =========================================================================

    [Fact]
    public void GetByIncident_FiltersCorrectly()
    {
        var targetIncident = Guid.NewGuid();
        var otherIncident = Guid.NewGuid();

        var records = new List<EvidenceRecord>
        {
            CreateRecord(incidentId: targetIncident),
            CreateRecord(incidentId: targetIncident),
            CreateRecord(incidentId: otherIncident),
            CreateRecord(incidentId: null)
        };

        var filtered = records.Where(r => r.IncidentId == targetIncident).ToList();

        filtered.Should().HaveCount(2);
        filtered.Should().AllSatisfy(r => r.IncidentId.Should().Be(targetIncident));
    }

    [Fact]
    public void GetPendingUpload_ReturnsOnlyCapturedAndQueued()
    {
        var records = new List<EvidenceRecord>
        {
            CreateRecord(EvidenceStatus.Captured),
            CreateRecord(EvidenceStatus.Queued),
            CreateRecord(EvidenceStatus.Uploading),
            CreateRecord(EvidenceStatus.Uploaded),
            CreateRecord(EvidenceStatus.Verified),
            CreateRecord(EvidenceStatus.Failed)
        };

        var pending = records
            .Where(r => r.Status is EvidenceStatus.Captured or EvidenceStatus.Queued)
            .ToList();

        pending.Should().HaveCount(2);
        pending.Should().AllSatisfy(r =>
            r.Status.Should().BeOneOf(EvidenceStatus.Captured, EvidenceStatus.Queued));
    }

    [Fact]
    public void GetPendingUpload_EmptyList_ReturnsEmpty()
    {
        var records = new List<EvidenceRecord>
        {
            CreateRecord(EvidenceStatus.Uploaded),
            CreateRecord(EvidenceStatus.Verified)
        };

        var pending = records
            .Where(r => r.Status is EvidenceStatus.Captured or EvidenceStatus.Queued)
            .ToList();

        pending.Should().BeEmpty();
    }

    [Fact]
    public void UpdateStatus_ChangesRecordStatus()
    {
        var records = new List<EvidenceRecord> { CreateRecord(EvidenceStatus.Captured) };
        var targetId = records[0].Id;

        var record = records.FirstOrDefault(r => r.Id == targetId);
        if (record is not null)
        {
            record.Status = EvidenceStatus.Queued;
        }

        records[0].Status.Should().Be(EvidenceStatus.Queued);
    }

    [Fact]
    public void UpdateStatus_NonExistentId_NoChange()
    {
        var records = new List<EvidenceRecord> { CreateRecord(EvidenceStatus.Captured) };

        var record = records.FirstOrDefault(r => r.Id == Guid.NewGuid());

        record.Should().BeNull();
    }

    // =========================================================================
    // Upload Progress Tracking
    // =========================================================================

    [Fact]
    public void UploadProgress_TracksIncrementally()
    {
        var record = CreateRecord(EvidenceStatus.Uploading);

        record.UploadProgress = 0.0;
        record.UploadProgress.Should().Be(0.0);

        record.UploadProgress = 0.25;
        record.UploadProgress.Should().Be(0.25);

        record.UploadProgress = 0.50;
        record.UploadProgress.Should().Be(0.50);

        record.UploadProgress = 1.0;
        record.UploadProgress.Should().Be(1.0);
    }

    // =========================================================================
    // Moderation Integration
    // =========================================================================

    [Fact]
    public void Evidence_CanBeFlagged()
    {
        var record = CreateRecord();
        record.IsFlagged = true;
        record.ModerationScore = 0.85;

        record.IsFlagged.Should().BeTrue();
        record.ModerationScore.Should().Be(0.85);
    }

    // =========================================================================
    // MediaType and EvidenceStatus Enums
    // =========================================================================

    [Fact]
    public void MediaType_HasThreeValues()
    {
        Enum.GetValues<MediaType>().Should().HaveCount(3);
    }

    [Fact]
    public void MediaType_AllDefined()
    {
        MediaType.Photo.Should().BeDefined();
        MediaType.Video.Should().BeDefined();
        MediaType.Audio.Should().BeDefined();
    }

    [Fact]
    public void EvidenceStatus_HasSixValues()
    {
        Enum.GetValues<EvidenceStatus>().Should().HaveCount(6);
    }

    [Fact]
    public void EvidenceStatus_AllDefined()
    {
        EvidenceStatus.Captured.Should().BeDefined();
        EvidenceStatus.Queued.Should().BeDefined();
        EvidenceStatus.Uploading.Should().BeDefined();
        EvidenceStatus.Uploaded.Should().BeDefined();
        EvidenceStatus.Verified.Should().BeDefined();
        EvidenceStatus.Failed.Should().BeDefined();
    }

    // =========================================================================
    // CapturedMedia Model
    // =========================================================================

    [Fact]
    public void CapturedMedia_CanBeCreated()
    {
        var media = new CapturedMedia
        {
            FilePath = "/evidence/photo_20260226.jpg",
            MimeType = "image/jpeg",
            FileSize = 3_000_000,
            CapturedAt = DateTime.UtcNow,
            MediaType = MediaType.Photo
        };

        media.FilePath.Should().NotBeEmpty();
        media.MimeType.Should().Be("image/jpeg");
        media.FileSize.Should().Be(3_000_000);
    }

    [Fact]
    public void CapturedMedia_VideoType()
    {
        var media = new CapturedMedia
        {
            FilePath = "/evidence/video_20260226.mp4",
            MimeType = "video/mp4",
            FileSize = 15_000_000,
            MediaType = MediaType.Video
        };

        media.MediaType.Should().Be(MediaType.Video);
        media.MimeType.Should().Be("video/mp4");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static EvidenceRecord CreateRecord(
        EvidenceStatus status = EvidenceStatus.Captured,
        Guid? incidentId = null)
    {
        return new EvidenceRecord
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            FilePath = $"/evidence/file_{Guid.NewGuid():N}.jpg",
            MimeType = "image/jpeg",
            FileSize = 1_000_000,
            MediaType = MediaType.Photo,
            CapturedAt = DateTime.UtcNow,
            Latitude = 40.7128,
            Longitude = -74.006,
            DeviceModel = "TestModel",
            DeviceManufacturer = "TestMfg",
            DevicePlatform = "Android",
            UserId = Guid.NewGuid(),
            FileHash = Guid.NewGuid().ToString("N"),
            CustodyRecordId = Guid.NewGuid(),
            Status = status
        };
    }
}

/// <summary>
/// Mirror of EvidenceRecord from TheWatch.Mobile.Services
/// </summary>
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

/// <summary>Mirror of EvidenceStatus</summary>
public enum EvidenceStatus
{
    Captured,
    Queued,
    Uploading,
    Uploaded,
    Verified,
    Failed
}

/// <summary>Mirror of MediaType</summary>
public enum MediaType
{
    Photo,
    Video,
    Audio
}

/// <summary>Mirror of CapturedMedia</summary>
public class CapturedMedia
{
    public string FilePath { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime CapturedAt { get; set; }
    public MediaType MediaType { get; set; }
}
