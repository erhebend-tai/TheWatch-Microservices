using Microsoft.Extensions.Logging;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Camera integration for evidence photo/video capture.
/// Wraps MAUI Essentials MediaPicker with permission handling.
/// Returns CapturedMedia records with file path, MIME type, size, and timestamp.
/// </summary>
public class CameraService
{
    private readonly ILogger<CameraService> _logger;

    public CameraService(ILogger<CameraService> logger)
    {
        _logger = logger;
    }

    public async Task<CapturedMedia?> CapturePhotoAsync()
    {
        if (!MediaPicker.IsCaptureSupported)
        {
            _logger.LogWarning("Photo capture not supported on this device");
            return null;
        }

        var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
        if (cameraStatus != PermissionStatus.Granted)
        {
            _logger.LogWarning("Camera permission denied");
            return null;
        }

        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo is null) return null;

            // Copy to app data directory for persistence
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "evidence", $"photo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            await using var sourceStream = await photo.OpenReadAsync();
            await using var targetStream = File.Create(targetPath);
            await sourceStream.CopyToAsync(targetStream);

            var fileInfo = new FileInfo(targetPath);
            _logger.LogInformation("Photo captured: {Path} ({Size} bytes)", targetPath, fileInfo.Length);

            return new CapturedMedia
            {
                FilePath = targetPath,
                MimeType = "image/jpeg",
                FileSize = fileInfo.Length,
                CapturedAt = DateTime.UtcNow,
                MediaType = MediaType.Photo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture photo");
            return null;
        }
    }

    public async Task<CapturedMedia?> CaptureVideoAsync()
    {
        if (!MediaPicker.IsCaptureSupported)
        {
            _logger.LogWarning("Video capture not supported on this device");
            return null;
        }

        var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
        if (cameraStatus != PermissionStatus.Granted)
        {
            _logger.LogWarning("Camera permission denied");
            return null;
        }

        try
        {
            var video = await MediaPicker.CaptureVideoAsync();
            if (video is null) return null;

            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "evidence", $"video_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            await using var sourceStream = await video.OpenReadAsync();
            await using var targetStream = File.Create(targetPath);
            await sourceStream.CopyToAsync(targetStream);

            var fileInfo = new FileInfo(targetPath);
            _logger.LogInformation("Video captured: {Path} ({Size} bytes)", targetPath, fileInfo.Length);

            return new CapturedMedia
            {
                FilePath = targetPath,
                MimeType = "video/mp4",
                FileSize = fileInfo.Length,
                CapturedAt = DateTime.UtcNow,
                MediaType = MediaType.Video
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture video");
            return null;
        }
    }

    public async Task<CapturedMedia?> PickPhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo is null) return null;

            var fileInfo = new FileInfo(photo.FullPath);
            return new CapturedMedia
            {
                FilePath = photo.FullPath,
                MimeType = photo.ContentType,
                FileSize = fileInfo.Length,
                CapturedAt = DateTime.UtcNow,
                MediaType = MediaType.Photo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pick photo");
            return null;
        }
    }
}

public class CapturedMedia
{
    public string FilePath { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime CapturedAt { get; set; }
    public MediaType MediaType { get; set; }
}

public enum MediaType
{
    Photo,
    Video,
    Audio
}
