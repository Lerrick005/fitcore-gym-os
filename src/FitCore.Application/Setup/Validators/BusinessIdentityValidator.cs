using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class BusinessIdentityValidator : AbstractValidator<BrandingSettings>
{
    public BusinessIdentityValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(200);
        RuleFor(x => x.TradingName).MaximumLength(200);
        RuleFor(x => x.Tagline).MaximumLength(200);
    }
}
