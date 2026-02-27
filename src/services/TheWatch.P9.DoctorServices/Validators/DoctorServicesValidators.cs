using FluentValidation;
using TheWatch.P9.DoctorServices.Doctors;

namespace TheWatch.P9.DoctorServices.Validators;

/// <summary>STIG V-222606: Input validation for DoctorServices request DTOs.</summary>

public class CreateDoctorProfileRequestValidator : AbstractValidator<CreateDoctorProfileRequest>
{
    public CreateDoctorProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Specializations).NotEmpty()
            .WithMessage("At least one specialization is required");
        RuleForEach(x => x.Specializations).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue);
    }
}

public class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Appointment must be in the future");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 240)
            .WithMessage("Duration must be between 5 minutes and 4 hours");
        RuleFor(x => x.Notes).MaximumLength(4096);
    }
}

public class UpdateAppointmentStatusRequestValidator : AbstractValidator<UpdateAppointmentStatusRequest>
{
    public UpdateAppointmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class RescheduleRequestValidator : AbstractValidator<RescheduleRequest>
{
    public RescheduleRequestValidator()
    {
        RuleFor(x => x.NewScheduledAt).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Rescheduled time must be in the future");
    }
}

public class DoctorSearchQueryValidator : AbstractValidator<DoctorSearchQuery>
{
    public DoctorSearchQueryValidator()
    {
        RuleFor(x => x.Specialization).MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90.0, 90.0).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180.0, 180.0).When(x => x.Longitude.HasValue);
        RuleFor(x => x.RadiusKm).GreaterThan(0).LessThanOrEqualTo(500).When(x => x.RadiusKm.HasValue);
        // If latitude is provided, longitude must also be provided and vice versa
        RuleFor(x => x.Longitude).NotNull()
            .When(x => x.Latitude.HasValue)
            .WithMessage("Longitude is required when latitude is provided");
        RuleFor(x => x.Latitude).NotNull()
            .When(x => x.Longitude.HasValue)
            .WithMessage("Latitude is required when longitude is provided");
    }
}
