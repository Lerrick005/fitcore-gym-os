using System.Globalization;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Settings;

namespace FitCore.Infrastructure.Formatting;

public sealed class LocaleDateTimeFormatter : IDateTimeFormatter
{
    private readonly ISettingsService _settings;

    public LocaleDateTimeFormatter(ISettingsService settings) => _settings = settings;

    public async Task<string> FormatDateAsync(DateTime utcInstant, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var local = ToLocal(utcInstant, locale);
        return local.ToString(locale.DateFormat, CultureInfo.InvariantCulture);
    }

    public async Task<string> FormatTimeAsync(DateTime utcInstant, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var local = ToLocal(utcInstant, locale);
        return local.ToString(locale.TimeFormat, CultureInfo.InvariantCulture);
    }

    public async Task<string> FormatDateTimeAsync(DateTime utcInstant, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var local = ToLocal(utcInstant, locale);
        return $"{local.ToString(locale.DateFormat, CultureInfo.InvariantCulture)} {local.ToString(locale.TimeFormat, CultureInfo.InvariantCulture)}";
    }

    public async Task<DateTime> GetBusinessDayStartUtcAsync(DateTime utcInstant, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var tz = TryFindTimeZone(locale.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc), tz);
        var localMidnight = local.Date;
        var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localMidnight, DateTimeKind.Unspecified), tz);
        return utcMidnight;
    }

    private static DateTime ToLocal(DateTime utcInstant, LocaleSettings locale)
    {
        var tz = TryFindTimeZone(locale.TimeZoneId);
        var utc = DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    private static TimeZoneInfo TryFindTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}

