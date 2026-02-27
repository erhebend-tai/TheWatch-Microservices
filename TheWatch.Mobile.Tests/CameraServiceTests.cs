using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for camera service logic — file path generation, MIME type mapping,
/// and CapturedMedia model defaults.
/// Since CameraService depends on MAUI MediaPicker and FileSystem,
/// we test the pure path/naming algorithms independently.
/// Note: CapturedMedia and MediaType mirror types are defined in EvidenceMetadataTests.cs
/// and shared across this namespace — no duplicate definitions needed here.
/// </summary>
public class CameraServiceTests
{
    // =========================================================================
    // File Path Generation
    // =========================================================================

    [Fact]
    public void PhotoPath_IncludesTimestampFormat()
    {
        var timestamp = new DateTime(2026, 3, 15, 14, 30, 45, DateTimeKind.Utc);

        var path = GeneratePhotoPath(timestamp);

        path.Should().Contain("20260315_143045");
    }

    [Fact]
    public void VideoPath_IncludesMp4Extension()
    {
        var timestamp = DateTime.UtcNow;

        var path = GenerateVideoPath(timestamp);

        path.Should().EndWith(".mp4");
    }

    [Fact]
    public void PhotoPath_StoredInEvidenceSubdirectory()
    {
        var timestamp = DateTime.UtcNow;

        var path = GeneratePhotoPath(timestamp);

        path.Should().Contain("evidence/");
    }

    [Fact]
    public void PhotoPath_HasJpgExtension()
    {
        var timestamp = DateTime.UtcNow;

        var path = GeneratePhotoPath(timestamp);

        path.Should().EndWith(".jpg");
    }

    [Fact]
    public void VideoPath_StoredInEvidenceSubdirectory()
    {
        var timestamp = DateTime.UtcNow;

        var path = GenerateVideoPath(timestamp);

        path.Should().Contain("evidence/");
    }

    // =========================================================================
    // CapturedMedia Model Defaults
    // =========================================================================

    [Fact]
    public void CapturedMedia_Defaults()
    {
        var media = new CapturedMedia();

        media.FilePath.Should().BeEmpty();
        media.MimeType.Should().BeEmpty();
        media.FileSize.Should().Be(0);
    }

    [Fact]
    public void CapturedMedia_PhotoType_CorrectMimeType()
    {
        var mimeType = GetMimeType(MediaType.Photo);

        mimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public void CapturedMedia_VideoType_CorrectMimeType()
    {
        var mimeType = GetMimeType(MediaType.Video);

        mimeType.Should().Be("video/mp4");
    }

    [Fact]
    public void CapturedMedia_AudioType_CorrectMimeType()
    {
        var mimeType = GetMimeType(MediaType.Audio);

        mimeType.Should().Be("audio/m4a");
    }

    // =========================================================================
    // MediaType Enum (already defined in EvidenceMetadataTests.cs)
    // =========================================================================

    [Fact]
    public void MediaType_HasThreeValues_Photo_Video_Audio()
    {
        Enum.GetValues<MediaType>().Should().HaveCount(3);
        MediaType.Photo.Should().BeDefined();
        MediaType.Video.Should().BeDefined();
        MediaType.Audio.Should().BeDefined();
    }

    // =========================================================================
    // Mirrors CameraService path generation logic
    // =========================================================================

    private static string GeneratePhotoPath(DateTime timestamp)
    {
        var fileName = $"photo_{timestamp:yyyyMMdd_HHmmss}.jpg";
        return $"evidence/{fileName}";
    }

    private static string GenerateVideoPath(DateTime timestamp)
    {
        var fileName = $"video_{timestamp:yyyyMMdd_HHmmss}.mp4";
        return $"evidence/{fileName}";
    }

    private static string GetMimeType(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Photo => "image/jpeg",
            MediaType.Video => "video/mp4",
            MediaType.Audio => "audio/m4a",
            _ => "application/octet-stream"
        };
    }
}
