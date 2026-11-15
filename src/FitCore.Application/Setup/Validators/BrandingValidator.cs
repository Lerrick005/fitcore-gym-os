using System.Text.RegularExpressions;
using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class BrandingValidator : AbstractValidator<BrandingSettings>
{
    private static readonly Regex Hex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public BrandingValidator()
    {
        RuleFor(x => x.PrimaryColor).Must(BeHex).WithMessage("Primary color must be #RRGGBB.");
        RuleFor(x => x.SecondaryColor).Must(BeHex).WithMessage("Secondary color must be #RRGGBB.");
        RuleFor(x => x.AccentColor).Must(BeHex).WithMessage("Accent color must be #RRGGBB.");
    }

    private static bool BeHex(string? v) => !string.IsNullOrEmpty(v) && Hex.IsMatch(v);
}
