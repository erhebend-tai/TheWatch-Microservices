using FluentValidation;
using TheWatch.P3.MeshNetwork.Mesh;

namespace TheWatch.P3.MeshNetwork.Validators;

/// <summary>STIG V-222606: Input validation for MeshNetwork request DTOs.</summary>

public class RegisterNodeRequestValidator : AbstractValidator<RegisterNodeRequest>
{
    public RegisterNodeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DeviceId).MaximumLength(128);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue);
        RuleFor(x => x.BatteryPercent).InclusiveBetween(0, 100).When(x => x.BatteryPercent.HasValue);
    }
}

public class UpdateNodeStatusRequestValidator : AbstractValidator<UpdateNodeStatusRequest>
{
    public UpdateNodeStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue);
        RuleFor(x => x.BatteryPercent).InclusiveBetween(0, 100).When(x => x.BatteryPercent.HasValue);
    }
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.SenderId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Priority).IsInEnum();
        // Must have at least a recipient or a channel
        RuleFor(x => x)
            .Must(x => x.RecipientId.HasValue || x.ChannelId.HasValue)
            .WithMessage("Either RecipientId or ChannelId must be specified");
    }
}

public class CreateChannelRequestValidator : AbstractValidator<CreateChannelRequest>
{
    public CreateChannelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
    }
}
