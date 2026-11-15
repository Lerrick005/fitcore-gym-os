using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class GymTypePreferencesValidator : AbstractValidator<GymPreferencesSettings>
{
    private static readonly HashSet<string> ValidGymTypes = new(StringComparer.Ordinal)
    {
        "general", "crossfit", "women-only", "boxing-mma", "yoga-studio", "powerlifting", "multi-facility"
    };

    private static readonly HashSet<string> ValidCheckInMethods = new(StringComparer.Ordinal)
    {
        "manual_search", "member_number", "qr_code"
    };

    public GymTypePreferencesValidator()
    {
        RuleFor(x => x.GymType)
            .NotEmpty().WithMessage("Gym type is required.")
            .Must(t => ValidGymTypes.Contains(t)).WithMessage("Gym type is not recognised.");

        RuleFor(x => x.CheckInMethod)
            .NotEmpty()
            .Must(m => ValidCheckInMethods.Contains(m)).WithMessage("Check-in method is not recognised.");

        RuleFor(x => x.RequiredMemberFields)
            .NotNull()
            .Must(f => f.Contains("FullName") && f.Contains("Phone"))
            .WithMessage("Full name and phone are required member fields.");
    }
}
