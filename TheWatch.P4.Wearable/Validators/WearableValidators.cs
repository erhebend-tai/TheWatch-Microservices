using FluentValidation;
using TheWatch.P4.Wearable.Devices;

namespace TheWatch.P4.Wearable.Validators;

/// <summary>STIG V-222606: Input validation for Wearable request DTOs.</summary>

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.FirmwareVersion).MaximumLength(50);
    }
}

public class UpdateDeviceStatusRequestValidator : AbstractValidator<UpdateDeviceStatusRequest>
{
    public UpdateDeviceStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.BatteryPercent).InclusiveBetween(0, 100).When(x => x.BatteryPercent.HasValue);
    }
}

public class RecordHeartbeatRequestValidator : AbstractValidator<RecordHeartbeatRequest>
{
    public RecordHeartbeatRequestValidator()
    {
        RuleFor(x => x.Bpm).InclusiveBetween(20, 300)
            .WithMessage("Heart rate must be between 20 and 300 BPM");
        RuleFor(x => x.StepCount).GreaterThanOrEqualTo(0).When(x => x.StepCount.HasValue)
            .WithMessage("Step count cannot be negative");
        RuleFor(x => x.CaloriesBurned).GreaterThanOrEqualTo(0).When(x => x.CaloriesBurned.HasValue)
            .WithMessage("Calories burned cannot be negative");
    }
}

public class StartSyncRequestValidator : AbstractValidator<StartSyncRequest>
{
    public StartSyncRequestValidator()
    {
        RuleFor(x => x.Direction).IsInEnum();
    }
}
