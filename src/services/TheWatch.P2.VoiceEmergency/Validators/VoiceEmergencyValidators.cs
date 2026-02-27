using FluentValidation;
using TheWatch.P2.VoiceEmergency.Emergency;

namespace TheWatch.P2.VoiceEmergency.Validators;

/// <summary>STIG V-222606: Input validation for VoiceEmergency request DTOs.</summary>

public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Location).NotNull();
        RuleFor(x => x.Location.Latitude).InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Location.Longitude).InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180");
        RuleFor(x => x.ReporterId).NotEmpty();
        RuleFor(x => x.ReporterName).MaximumLength(255);
        RuleFor(x => x.ReporterPhone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.ReporterPhone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.Severity).InclusiveBetween(1, 5)
            .WithMessage("Severity must be between 1 (lowest) and 5 (highest)");
    }
}

public class UpdateIncidentStatusRequestValidator : AbstractValidator<UpdateIncidentStatusRequest>
{
    public UpdateIncidentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(2000);
    }
}

public class CreateDispatchRequestValidator : AbstractValidator<CreateDispatchRequest>
{
    public CreateDispatchRequestValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500)
            .WithMessage("Dispatch radius must be between 0 and 500 km");
        RuleFor(x => x.RespondersRequested).InclusiveBetween(1, 100)
            .WithMessage("Responders requested must be between 1 and 100");
    }
}

public class ExpandRadiusRequestValidator : AbstractValidator<ExpandRadiusRequest>
{
    public ExpandRadiusRequestValidator()
    {
        RuleFor(x => x.AdditionalKm).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Additional radius must be between 0 and 100 km");
    }
}
