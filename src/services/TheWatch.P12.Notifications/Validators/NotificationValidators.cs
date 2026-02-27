using FluentValidation;
using TheWatch.P12.Notifications.Notifications;

namespace TheWatch.P12.Notifications.Validators;

public class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DeepLink).MaximumLength(2048).When(x => x.DeepLink is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(2048).When(x => x.ImageUrl is not null);
    }
}

public class BroadcastRequestValidator : AbstractValidator<BroadcastRequest>
{
    public BroadcastRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RadiusKm).GreaterThan(0).When(x => x.RadiusKm.HasValue);
        RuleFor(x => x.TargetLatitude).InclusiveBetween(-90, 90).When(x => x.TargetLatitude.HasValue);
        RuleFor(x => x.TargetLongitude).InclusiveBetween(-180, 180).When(x => x.TargetLongitude.HasValue);
    }
}

public class SetNotificationPreferenceRequestValidator : AbstractValidator<SetNotificationPreferenceRequest>
{
    public SetNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QuietHoursStart)
            .Matches(@"^([01]\d|2[0-3]):([0-5]\d)$")
            .When(x => x.QuietHoursStart is not null)
            .WithMessage("QuietHoursStart must be in HH:mm format");
        RuleFor(x => x.QuietHoursEnd)
            .Matches(@"^([01]\d|2[0-3]):([0-5]\d)$")
            .When(x => x.QuietHoursEnd is not null)
            .WithMessage("QuietHoursEnd must be in HH:mm format");
    }
}
