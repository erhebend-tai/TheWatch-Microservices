using Microsoft.EntityFrameworkCore;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Data.Seeders;

public class SurveillanceSeeder : IWatchDataSeeder
{
    public async Task SeedAsync(SurveillanceDbContext context, CancellationToken ct = default)
    {
        if (await context.Set<CameraRegistration>().AnyAsync(ct))
            return;

        var userId1 = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var userId2 = Guid.Parse("00000000-0000-0000-0000-000000010002");
        var userId3 = Guid.Parse("00000000-0000-0000-0000-000000010003");

        // Cameras
        var cameras = new[]
        {
            new CameraRegistration { Id = Guid.Parse("00000000-0000-0000-0011-000000000001"), OwnerId = userId1, Latitude = 34.0522, Longitude = -118.2437, Address = "123 Main St, Los Angeles, CA", CoverageRadiusMeters = 100, CameraModel = "Ring Doorbell Pro", IsPublic = true, Status = CameraStatus.Verified, Source = SubmissionSource.Doorbell, Description = "Front entrance camera", VerifiedAt = DateTime.UtcNow.AddDays(-30) },
            new CameraRegistration { Id = Guid.Parse("00000000-0000-0000-0011-000000000002"), OwnerId = userId1, Latitude = 34.0525, Longitude = -118.2440, Address = "123 Main St, Los Angeles, CA", CoverageRadiusMeters = 50, CameraModel = "Nest Cam IQ", IsPublic = true, Status = CameraStatus.Verified, Source = SubmissionSource.PrivateCamera, Description = "Backyard camera", VerifiedAt = DateTime.UtcNow.AddDays(-28) },
            new CameraRegistration { Id = Guid.Parse("00000000-0000-0000-0011-000000000003"), OwnerId = userId2, Latitude = 34.0530, Longitude = -118.2450, Address = "456 Oak Ave, Los Angeles, CA", CoverageRadiusMeters = 200, CameraModel = "Axis P1448-LE", IsPublic = false, Status = CameraStatus.Active, Source = SubmissionSource.PublicCamera, Description = "Parking lot surveillance" },
            new CameraRegistration { Id = Guid.Parse("00000000-0000-0000-0011-000000000004"), OwnerId = userId3, Latitude = 34.0540, Longitude = -118.2460, CoverageRadiusMeters = 75, CameraModel = "Tesla Dashcam", IsPublic = true, Status = CameraStatus.Pending, Source = SubmissionSource.Dashcam, Description = "Mobile dashcam" },
            new CameraRegistration { Id = Guid.Parse("00000000-0000-0000-0011-000000000005"), OwnerId = userId2, Latitude = 34.0535, Longitude = -118.2445, Address = "456 Oak Ave, Los Angeles, CA", CoverageRadiusMeters = 150, CameraModel = "DJI Mavic 3", IsPublic = false, Status = CameraStatus.Verified, Source = SubmissionSource.Drone, Description = "Aerial surveillance drone", VerifiedAt = DateTime.UtcNow.AddDays(-7) }
        };
        context.Set<CameraRegistration>().AddRange(cameras);

        // Footage submissions
        var footage = new[]
        {
            new FootageSubmission { Id = Guid.Parse("00000000-0000-0000-0011-000000000010"), CameraId = cameras[0].Id, SubmitterId = userId1, GpsLatitude = 34.0522, GpsLongitude = -118.2437, GpsVerified = true, StartTime = DateTime.UtcNow.AddHours(-6), EndTime = DateTime.UtcNow.AddHours(-5), MediaUrl = "https://storage.thewatch.app/footage/001.mp4", DurationSeconds = 3600, Status = FootageStatus.Analyzed, Description = "Morning activity", AnalysisCompletedAt = DateTime.UtcNow.AddHours(-4) },
            new FootageSubmission { Id = Guid.Parse("00000000-0000-0000-0011-000000000011"), CameraId = cameras[1].Id, SubmitterId = userId1, GpsLatitude = 34.0525, GpsLongitude = -118.2440, GpsVerified = true, StartTime = DateTime.UtcNow.AddHours(-3), EndTime = DateTime.UtcNow.AddHours(-2), MediaUrl = "https://storage.thewatch.app/footage/002.mp4", DurationSeconds = 3600, Status = FootageStatus.Submitted, Description = "Backyard motion detected" },
            new FootageSubmission { Id = Guid.Parse("00000000-0000-0000-0011-000000000012"), CameraId = cameras[2].Id, SubmitterId = userId2, GpsLatitude = 34.0530, GpsLongitude = -118.2450, GpsVerified = true, StartTime = DateTime.UtcNow.AddHours(-12), EndTime = DateTime.UtcNow.AddHours(-11), MediaUrl = "https://storage.thewatch.app/footage/003.mp4", DurationSeconds = 3600, Status = FootageStatus.Verified, Description = "Late night parking lot activity", AnalysisCompletedAt = DateTime.UtcNow.AddHours(-10) }
        };
        context.Set<FootageSubmission>().AddRange(footage);

        // Crime locations
        var crimeLocations = new[]
        {
            new CrimeLocation { Id = Guid.Parse("00000000-0000-0000-0011-000000000020"), Latitude = 34.0528, Longitude = -118.2442, Description = "Vehicle break-in reported", CrimeType = "Burglary", ReporterId = userId1, OccurredAt = DateTime.UtcNow.AddDays(-2), IsActive = true },
            new CrimeLocation { Id = Guid.Parse("00000000-0000-0000-0011-000000000021"), Latitude = 34.0535, Longitude = -118.2455, Description = "Suspicious activity near warehouse", CrimeType = "Trespassing", ReporterId = userId2, OccurredAt = DateTime.UtcNow.AddDays(-1), IsActive = true },
            new CrimeLocation { Id = Guid.Parse("00000000-0000-0000-0011-000000000022"), Latitude = 34.0540, Longitude = -118.2460, Description = "Package theft from porch", CrimeType = "Theft", ReporterId = userId3, OccurredAt = DateTime.UtcNow.AddHours(-8), IsActive = true }
        };
        context.Set<CrimeLocation>().AddRange(crimeLocations);

        // Detection results (for analyzed footage)
        var detections = new[]
        {
            new DetectionResult { Id = Guid.Parse("00000000-0000-0000-0011-000000000030"), FootageId = footage[0].Id, DetectionType = DetectionType.Person, Label = "person", Confidence = 0.95f, BoundingBoxX = 0.2f, BoundingBoxY = 0.3f, BoundingBoxW = 0.15f, BoundingBoxH = 0.4f, FrameTimestamp = footage[0].StartTime.AddMinutes(15), ModelVersion = "yolov8n-1.0" },
            new DetectionResult { Id = Guid.Parse("00000000-0000-0000-0011-000000000031"), FootageId = footage[0].Id, DetectionType = DetectionType.Vehicle, Label = "car", Confidence = 0.89f, BoundingBoxX = 0.5f, BoundingBoxY = 0.6f, BoundingBoxW = 0.3f, BoundingBoxH = 0.25f, FrameTimestamp = footage[0].StartTime.AddMinutes(20), ModelVersion = "yolov8n-1.0" },
            new DetectionResult { Id = Guid.Parse("00000000-0000-0000-0011-000000000032"), FootageId = footage[2].Id, DetectionType = DetectionType.Person, Label = "person", Confidence = 0.91f, BoundingBoxX = 0.1f, BoundingBoxY = 0.2f, BoundingBoxW = 0.12f, BoundingBoxH = 0.35f, FrameTimestamp = footage[2].StartTime.AddMinutes(45), ModelVersion = "yolov8n-1.0" },
            new DetectionResult { Id = Guid.Parse("00000000-0000-0000-0011-000000000033"), FootageId = footage[2].Id, DetectionType = DetectionType.Package, Label = "package", Confidence = 0.78f, BoundingBoxX = 0.3f, BoundingBoxY = 0.7f, BoundingBoxW = 0.08f, BoundingBoxH = 0.1f, FrameTimestamp = footage[2].StartTime.AddMinutes(46), ModelVersion = "yolov8n-1.0" }
        };
        context.Set<DetectionResult>().AddRange(detections);

        await context.SaveChangesAsync(ct);
    }
}
