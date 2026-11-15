using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class LocaleValidator : AbstractValidator<LocaleSettings>
{
    public LocaleValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3).Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must be 3 letters (e.g. USD, KES).");
        RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(5);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 4);
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .Must(BeKnownTimeZone).WithMessage("Time zone is not recognised.");
        RuleFor(x => x.DateFormat).NotEmpty();
        RuleFor(x => x.TimeFormat).NotEmpty();
    }

    private static bool BeKnownTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch { return false; }
    }
}
