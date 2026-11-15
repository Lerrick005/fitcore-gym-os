using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class PlansValidator : AbstractValidator<PlansSettings>
{
    public PlansValidator()
    {
        RuleFor(x => x.Templates)
            .NotNull().WithMessage("Plans list is required.")
            .Must(t => t.Any(p => p.IsActive)).WithMessage("At least one active plan is required.");

        RuleForEach(x => x.Templates).SetValidator(new PlanTemplateValidator()!);
    }

    private sealed class PlanTemplateValidator : AbstractValidator<PlanTemplate>
    {
        public PlanTemplateValidator()
        {
            RuleFor(p => p.Name).NotEmpty().MaximumLength(200);
            RuleFor(p => p.PriceMinor).GreaterThanOrEqualTo(0);
            RuleFor(p => p.DurationValue).GreaterThan(0).WithMessage("Duration must be at least 1.");
            RuleFor(p => p.DurationUnit).InclusiveBetween(0, 2).WithMessage("Duration unit must be Days/Weeks/Months.");
            When(p => p.JoiningFeeMinor.HasValue, () =>
            {
                RuleFor(p => p.JoiningFeeMinor!.Value).GreaterThanOrEqualTo(0);
            });
        }
    }
}
