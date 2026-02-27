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
    public SubmitFootageRequestValidator()
    {
        RuleFor(x => x.CameraId).NotEmpty();
        RuleFor(x => x.SubmitterId).NotEmpty();
        RuleFor(x => x.GpsLatitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.GpsLongitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time");
        RuleFor(x => x.MediaUrl).NotEmpty().MaximumLength(1000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("Media URL must be a valid absolute URI");
        RuleFor(x => x.Description).MaximumLength(2000);
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
