using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

/// <summary>Business contact details validator (PRS §7). Wraps BusinessSettings.</summary>
public sealed class BusinessContactValidator : AbstractValidator<BusinessSettings>
{
    public BusinessContactValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email must be a valid email address.");
        RuleFor(x => x.Website).MaximumLength(300);
        RuleFor(x => x.PhysicalAddress).MaximumLength(500);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(100);
        RuleFor(x => x.PhonePrimary).MaximumLength(30);
        RuleFor(x => x.PhoneSecondary).MaximumLength(30);
    }
}
