using FluentValidation;
using TheWatch.P11.Surveillance.Surveillance;

namespace TheWatch.P11.Surveillance.Validators;

/// <summary>STIG V-222606: Input validation for Surveillance request DTOs.</summary>

public class RegisterCameraRequestValidator : AbstractValidator<RegisterCameraRequest>
{
    public RegisterCameraRequestValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.CoverageRadiusMeters).GreaterThan(0).LessThanOrEqualTo(5000)
            .WithMessage("Coverage radius must be between 0 and 5,000 meters");
        RuleFor(x => x.Heading).InclusiveBetween(0.0, 360.0).When(x => x.Heading.HasValue)
            .WithMessage("Heading must be between 0 and 360 degrees");
        RuleFor(x => x.CameraModel).MaximumLength(100);
        RuleFor(x => x.StreamUrl).MaximumLength(1000);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Source).IsInEnum();
    }
}

public class SubmitFootageRequestValidator : AbstractValidator<SubmitFootageRequest>
{
    private static readonly HashSet<string> ValidMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".mkv", ".webm", ".flv", ".wmv",  // Video
        ".mp3", ".wav", ".aac", ".ogg", ".flac", ".wma",          // Audio
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" // Image
    };

    public SubmitFootageRequestValidator()
    {
        // === Completeness: required fields ===
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.SubmitterId).NotEmpty();
        RuleFor(x => x.GpsLatitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.GpsLongitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.MediaType).IsInEnum()
            .WithMessage("MediaType must be a valid type (Video, Audio, Image)");

        // === Timeliness: StartTime must not be in the future ===
        RuleFor(x => x.StartTime).LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("StartTime cannot be in the future");

        // === Timeliness: StartTime must not be older than 30 days ===
        RuleFor(x => x.StartTime).GreaterThanOrEqualTo(DateTime.UtcNow.AddDays(-30))
            .WithMessage("Footage older than 30 days cannot be submitted");

        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time");

        // === Relevance: duration must be reasonable (max 24 hours) ===
        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime).TotalHours <= 24)
            .WithMessage("Footage duration cannot exceed 24 hours");

        // === Accuracy: valid media URL with recognized extension ===
        RuleFor(x => x.MediaUrl).NotEmpty().MaximumLength(1000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("Media URL must be a valid absolute URI");

        RuleFor(x => x.MediaUrl)
            .Must(BeRecognizedMediaExtension)
            .When(x => Uri.TryCreate(x.MediaUrl, UriKind.Absolute, out _))
            .WithMessage("Media URL must reference a recognized media file format");

        // === Accuracy: SHA-256 hash format if provided ===
        RuleFor(x => x.FileHashSha256)
            .Matches(@"^[a-fA-F0-9]{64}$")
            .When(x => !string.IsNullOrEmpty(x.FileHashSha256))
            .WithMessage("FileHashSha256 must be a valid 64-character hex string");

        RuleFor(x => x.Description).MaximumLength(2000);
    }

    private static bool BeRecognizedMediaExtension(string? url)
    {
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath;
        var ext = Path.GetExtension(path);
        return !string.IsNullOrEmpty(ext) && ValidMediaExtensions.Contains(ext);
    }
}

public class ReportCrimeLocationRequestValidator : AbstractValidator<ReportCrimeLocationRequest>
{
    public ReportCrimeLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.ReporterId).NotEmpty();
        RuleFor(x => x.CrimeType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OccurredAt).LessThanOrEqualTo(DateTime.UtcNow.AddHours(1))
            .When(x => x.OccurredAt.HasValue)
            .WithMessage("Occurrence time cannot be significantly in the future");
    }
}

public class SurveillanceSearchRequestValidator : AbstractValidator<SurveillanceSearchRequest>
{
    public SurveillanceSearchRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500);
        RuleFor(x => x.TimeWindowEnd).GreaterThan(x => x.TimeWindowStart)
            .When(x => x.TimeWindowStart.HasValue && x.TimeWindowEnd.HasValue)
            .WithMessage("Time window end must be after start");
    }
}

public class ObjectTrackingRequestValidator : AbstractValidator<ObjectTrackingRequest>
{
    public ObjectTrackingRequestValidator()
    {
        RuleFor(x => x.CrimeLocationId).NotEmpty();
        RuleFor(x => x.InitiatedBy).NotEmpty();
        RuleFor(x => x.ObjectDescription).NotEmpty().MaximumLength(4096)
            .WithMessage("Object description is required and must be under 4096 characters");
        RuleFor(x => x.SearchRadiusKm).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Search radius must be between 0 and 100 km");
        RuleFor(x => x.TimeWindowEnd).GreaterThan(x => x.TimeWindowStart)
            .When(x => x.TimeWindowStart.HasValue && x.TimeWindowEnd.HasValue)
            .WithMessage("Time window end must be after start");
        RuleFor(x => x.FootageMediaUrl).MaximumLength(1000)
            .Must(u => string.IsNullOrEmpty(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.FootageMediaUrl))
            .WithMessage("Footage media URL must be a valid absolute URI");
    }
}
