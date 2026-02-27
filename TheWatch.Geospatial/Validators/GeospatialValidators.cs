using FluentValidation;

namespace TheWatch.Geospatial.Validators;

/// <summary>STIG V-222606: Input validation for Geospatial request DTOs.</summary>

public class BoundingBoxQueryValidator : AbstractValidator<BoundingBoxQuery>
{
    public BoundingBoxQueryValidator()
    {
        RuleFor(x => x.MinLat).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.MaxLat).InclusiveBetween(-90.0, 90.0).GreaterThan(x => x.MinLat)
            .WithMessage("MaxLat must be greater than MinLat");
        RuleFor(x => x.MinLon).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.MaxLon).InclusiveBetween(-180.0, 180.0).GreaterThan(x => x.MinLon)
            .WithMessage("MaxLon must be greater than MinLon");
    }
}

public class NearbyQueryValidator : AbstractValidator<NearbyQuery>
{
    public NearbyQueryValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500)
            .WithMessage("Radius must be between 0 and 500 km");
    }
}

public class TrackEntityQueryValidator : AbstractValidator<TrackEntityQuery>
{
    public TrackEntityQueryValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.Speed).GreaterThanOrEqualTo(0).When(x => x.Speed.HasValue)
            .WithMessage("Speed cannot be negative");
        RuleFor(x => x.Heading).InclusiveBetween(0.0, 360.0).When(x => x.Heading.HasValue)
            .WithMessage("Heading must be between 0 and 360 degrees");
        RuleFor(x => x.Accuracy).GreaterThanOrEqualTo(0).When(x => x.Accuracy.HasValue)
            .WithMessage("Accuracy cannot be negative");
    }
}

// DTOs for validation (used by minimal API endpoints)
public record BoundingBoxQuery(double MinLat, double MaxLat, double MinLon, double MaxLon);
public record NearbyQuery(double Latitude, double Longitude, double RadiusKm);
public record TrackEntityQuery(Guid EntityId, double Latitude, double Longitude, double? Speed = null, double? Heading = null, double? Accuracy = null);
