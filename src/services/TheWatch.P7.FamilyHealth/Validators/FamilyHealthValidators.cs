using FluentValidation;
using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Validators;

/// <summary>STIG V-222606: Input validation for FamilyHealth request DTOs.</summary>

public class CreateFamilyGroupRequestValidator : AbstractValidator<CreateFamilyGroupRequest>
{
    public CreateFamilyGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
    }
}

public class CreateCheckInRequestValidator : AbstractValidator<CreateCheckInRequest>
{
    public CreateCheckInRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Message).MaximumLength(2000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90");
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180");
    }
}

public class RecordVitalRequestValidator : AbstractValidator<RecordVitalRequest>
{
    public RecordVitalRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0)
            .WithMessage("Vital reading value must be positive");
        RuleFor(x => x.Unit).MaximumLength(50);

        // Physiological range validation based on vital type
        RuleFor(x => x.Value)
            .InclusiveBetween(20, 300).When(x => x.Type == VitalType.HeartRate)
            .WithMessage("Heart rate must be between 20-300 BPM");
        RuleFor(x => x.Value)
            .InclusiveBetween(85.0, 110.0).When(x => x.Type == VitalType.Temperature)
            .WithMessage("Temperature must be between 85-110 degrees F");
        RuleFor(x => x.Value)
            .InclusiveBetween(0, 100).When(x => x.Type == VitalType.SpO2)
            .WithMessage("SpO2 must be between 0-100%");
        RuleFor(x => x.Value)
            .InclusiveBetween(4, 60).When(x => x.Type == VitalType.RespiratoryRate)
            .WithMessage("Respiratory rate must be between 4-60 breaths/min");
        RuleFor(x => x.Value)
            .InclusiveBetween(20, 600).When(x => x.Type == VitalType.BloodGlucose)
            .WithMessage("Blood glucose must be between 20-600 mg/dL");
    }
}
