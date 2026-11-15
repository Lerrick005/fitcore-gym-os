using System.Globalization;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Settings;
using FitCore.Domain.ValueObjects;

namespace FitCore.Infrastructure.Formatting;

public sealed class LocaleCurrencyFormatter : ICurrencyFormatter
{
    private readonly ISettingsService _settings;

    public LocaleCurrencyFormatter(ISettingsService settings) => _settings = settings;

    public async Task<string> FormatAsync(Money money, CancellationToken ct = default)
        => await FormatAsync(money.AmountMinor, money.CurrencyCode, ct);

    public async Task<string> FormatAsync(long amountMinor, string currencyCode, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var amount = amountMinor / Math.Pow(10, locale.DecimalPlaces);

        var numberFormat = new NumberFormatInfo
        {
            NumberDecimalDigits = locale.DecimalPlaces,
            NumberDecimalSeparator = locale.DecimalSeparator,
            NumberGroupSeparator = locale.ThousandsSeparator,
            NumberGroupSizes = new[] { 3 }
        };

        var formatted = amount.ToString("N", numberFormat);
        return locale.SymbolBeforeAmount
            ? $"{locale.CurrencySymbol} {formatted}"
            : $"{formatted} {locale.CurrencySymbol}";
    }
}
