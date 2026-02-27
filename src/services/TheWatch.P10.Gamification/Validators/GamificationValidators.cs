using FluentValidation;
using TheWatch.P10.Gamification.Gaming;

namespace TheWatch.P10.Gamification.Validators;

/// <summary>STIG V-222606: Input validation for Gamification request DTOs.</summary>

public class AwardPointsRequestValidator : AbstractValidator<AwardPointsRequest>
{
    public AwardPointsRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Points).GreaterThan(0).LessThanOrEqualTo(10000)
            .WithMessage("Points must be between 1 and 10,000");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class AwardBadgeRequestValidator : AbstractValidator<AwardBadgeRequest>
{
    public AwardBadgeRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Badge).NotEmpty().MaximumLength(100);
    }
}

public class CreateChallengeRequestValidator : AbstractValidator<CreateChallengeRequest>
{
    public CreateChallengeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TargetValue).GreaterThan(0).LessThanOrEqualTo(1000000)
            .WithMessage("Target value must be between 1 and 1,000,000");
        RuleFor(x => x.PointsReward).GreaterThan(0).LessThanOrEqualTo(10000)
            .WithMessage("Points reward must be between 1 and 10,000");
        RuleFor(x => x.BadgeReward).MaximumLength(100);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");
    }
}
