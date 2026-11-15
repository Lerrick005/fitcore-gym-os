using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class ReceiptValidator : AbstractValidator<ReceiptSettings>
{
    private static readonly HashSet<string> ValidPaperTypes = new(StringComparer.Ordinal)
    {
        "thermal58", "thermal80", "a4"
    };

    public ReceiptValidator()
    {
        RuleFor(x => x.PaperType)
            .NotEmpty()
            .Must(p => ValidPaperTypes.Contains(p)).WithMessage("Paper type must be thermal58, thermal80 or a4.");
        RuleFor(x => x.ReceiptNumberPrefix).NotEmpty().MaximumLength(20);
        RuleFor(x => x.HeaderText).MaximumLength(500);
        RuleFor(x => x.FooterText).MaximumLength(500);
    }
}
