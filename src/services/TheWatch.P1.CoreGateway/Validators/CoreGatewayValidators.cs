using FluentValidation;
using TheWatch.P1.CoreGateway.Core;
using TheWatch.Shared.Notifications;

namespace TheWatch.P1.CoreGateway.Validators;

/// <summary>STIG V-222606: Input validation for CoreGateway request DTOs.</summary>

public class CreateProfileRequestValidator : AbstractValidator<CreateProfileRequest>
{
    public CreateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).MaximumLength(255).When(x => x.DisplayName is not null);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue);
    }
}

public class SetPreferenceRequestValidator : AbstractValidator<SetPreferenceRequest>
{
    public SetPreferenceRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(255)
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Preference key must be alphanumeric with dots, underscores, or hyphens");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(4096);
    }
}

public class SetConfigRequestValidator : AbstractValidator<SetConfigRequest>
{
    public SetConfigRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(255)
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Config key must be alphanumeric with dots, underscores, or hyphens");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class DeviceRegistrationValidator : AbstractValidator<DeviceRegistration>
{
    public DeviceRegistrationValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeviceToken).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(50)
            .Must(p => new[] { "android", "ios", "windows", "macos", "linux" }.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Platform must be one of: android, ios, windows, macos, linux");
        RuleFor(x => x.DeviceModel).MaximumLength(100);
    }
}
