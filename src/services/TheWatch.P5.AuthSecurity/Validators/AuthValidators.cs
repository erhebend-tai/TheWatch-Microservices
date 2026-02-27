using FluentValidation;
using TheWatch.P5.AuthSecurity.Auth;

namespace TheWatch.P5.AuthSecurity.Validators;

/// <summary>STIG V-222606: Input validation for AuthSecurity request DTOs.</summary>

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256)
            .Must(e => e.Contains('@')).WithMessage("Valid email required");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(15).MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain digit")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain special character");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(1);
        RuleFor(x => x.TotpCode).MaximumLength(6)
            .Matches(@"^\d{6}$").When(x => !string.IsNullOrEmpty(x.TotpCode))
            .WithMessage("TOTP code must be exactly 6 digits");
        RuleFor(x => x.MfaToken).MaximumLength(500);
        RuleFor(x => x.DeviceFingerprint).MaximumLength(500);
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MinimumLength(10);
        RuleFor(x => x.DeviceFingerprint).MaximumLength(500);
    }
}

public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty().MaximumLength(50)
            .Must(r => new[] { "Admin", "Responder", "Doctor", "FamilyMember", "Patient", "ServiceAccount" }.Contains(r))
            .WithMessage("Role must be one of: Admin, Responder, Doctor, FamilyMember, Patient, ServiceAccount");
    }
}

public class MfaVerifyRequestValidator : AbstractValidator<MfaVerifyRequest>
{
    public MfaVerifyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(6)
            .Matches(@"^\d{6}$").WithMessage("MFA code must be exactly 6 digits");
    }
}

public class SmsMfaSendRequestValidator : AbstractValidator<SmsMfaSendRequest>
{
    public SmsMfaSendRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone must be in E.164 format");
    }
}

public class SmsMfaVerifyRequestValidator : AbstractValidator<SmsMfaVerifyRequest>
{
    public SmsMfaVerifyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(6)
            .Matches(@"^\d{6}$").WithMessage("Code must be exactly 6 digits");
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone must be in E.164 format");
    }
}

public class MagicLinkRequestValidator : AbstractValidator<MagicLinkRequest>
{
    public MagicLinkRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
