using FluentValidation;
using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Validators;

/// <summary>STIG V-222606: Input validation for FirstResponder request DTOs.</summary>

public class RegisterResponderRequestValidator : AbstractValidator<RegisterResponderRequest>
{
    public RegisterResponderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.BadgeNumber).MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.MaxResponseRadiusKm).GreaterThan(0).LessThanOrEqualTo(500)
            .WithMessage("Max response radius must be between 0 and 500 km");
    }
}

public class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.Accuracy).GreaterThanOrEqualTo(0).When(x => x.Accuracy.HasValue)
            .WithMessage("Accuracy cannot be negative");
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateCheckInRequestValidator : AbstractValidator<CreateCheckInRequest>
{
    public CreateCheckInRequestValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class NearbyResponderQueryValidator : AbstractValidator<NearbyResponderQuery>
{
    public NearbyResponderQueryValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
    }
}

public class SignupDesignatedResponderRequestValidator : AbstractValidator<SignupDesignatedResponderRequest>
{
    public SignupDesignatedResponderRequestValidator()
    {
        RuleFor(x => x.VolunteerName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.ResponseRadiusKm).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Response radius must be between 0 and 100 km");
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.LocationDescription).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateDesignatedResponderStatusRequestValidator : AbstractValidator<UpdateDesignatedResponderStatusRequest>
{
    public UpdateDesignatedResponderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
