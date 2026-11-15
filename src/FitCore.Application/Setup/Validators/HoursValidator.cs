using System.Globalization;
using FluentValidation;
using FitCore.Application.Settings;

namespace FitCore.Application.Setup.Validators;

public sealed class HoursValidator : AbstractValidator<HoursSettings>
{
    public HoursValidator()
    {
        RuleFor(x => x.Monday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Tuesday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Wednesday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Thursday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Friday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Saturday).SetValidator(new DayHoursValidator()!);
        RuleFor(x => x.Sunday).SetValidator(new DayHoursValidator()!);

        RuleFor(x => x).Must(HaveAtLeastOneOpenDay)
            .WithMessage("At least one day must be open.");
    }

    private static bool HaveAtLeastOneOpenDay(HoursSettings s)
    {
        return !s.Monday.IsClosed || !s.Tuesday.IsClosed || !s.Wednesday.IsClosed ||
               !s.Thursday.IsClosed || !s.Friday.IsClosed || !s.Saturday.IsClosed || !s.Sunday.IsClosed;
    }

    private sealed class DayHoursValidator : AbstractValidator<DayHours>
    {
        public DayHoursValidator()
        {
            When(x => !x.IsClosed, () =>
            {
                RuleFor(x => x.OpenTime).Must(BeTime).WithMessage("Open time must be HH:mm.");
                RuleFor(x => x.CloseTime).Must(BeTime).WithMessage("Close time must be HH:mm.");
                RuleFor(x => x).Must(OpenBeforeClose).WithMessage("Open time must be before close time.");
            });
        }

        private static bool BeTime(string? s)
            => TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

        private static bool OpenBeforeClose(DayHours d)
        {
            if (!TimeOnly.TryParseExact(d.OpenTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var o)) return true;
            if (!TimeOnly.TryParseExact(d.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var c)) return true;
            return o < c;
        }
    }
}
