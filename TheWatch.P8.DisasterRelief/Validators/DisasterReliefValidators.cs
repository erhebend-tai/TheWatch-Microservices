using FluentValidation;
using TheWatch.P8.DisasterRelief.Relief;

namespace TheWatch.P8.DisasterRelief.Validators;

/// <summary>STIG V-222606: Input validation for DisasterRelief request DTOs.</summary>

public class CreateDisasterEventRequestValidator : AbstractValidator<CreateDisasterEventRequest>
{
    public CreateDisasterEventRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(4096);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500)
            .WithMessage("Radius must be between 0 and 500 km");
        RuleFor(x => x.Severity).InclusiveBetween(1, 5)
            .WithMessage("Severity must be between 1 (lowest) and 5 (highest)");
    }
}

public class UpdateEventStatusRequestValidator : AbstractValidator<UpdateEventStatusRequest>
{
    public UpdateEventStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateShelterRequestValidator : AbstractValidator<CreateShelterRequest>
{
    public CreateShelterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.Capacity).GreaterThan(0).LessThanOrEqualTo(100000)
            .WithMessage("Capacity must be between 1 and 100,000");
        RuleFor(x => x.ContactPhone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Phone must be in E.164 format");
    }
}

public class UpdateOccupancyRequestValidator : AbstractValidator<UpdateOccupancyRequest>
{
    public UpdateOccupancyRequestValidator()
    {
        RuleFor(x => x.CurrentOccupancy).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100000)
            .WithMessage("Occupancy must be between 0 and 100,000");
    }
}

public class DonateResourceRequestValidator : AbstractValidator<DonateResourceRequest>
{
    public DonateResourceRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(1000000);
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
    }
}

public class CreateResourceRequestRecordValidator : AbstractValidator<CreateResourceRequestRecord>
{
    public CreateResourceRequestRecordValidator()
    {
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(1000000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
    }
}

public class CreateEvacuationRouteRequestValidator : AbstractValidator<CreateEvacuationRouteRequest>
{
    public CreateEvacuationRouteRequestValidator()
    {
        RuleFor(x => x.DisasterEventId).NotEmpty();
        RuleFor(x => x.OriginLat).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.OriginLon).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.DestLat).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.DestLon).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.DistanceKm).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.EstimatedTimeMinutes).GreaterThan(0).LessThanOrEqualTo(1440)
            .WithMessage("Estimated time must be between 1 minute and 24 hours");
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
