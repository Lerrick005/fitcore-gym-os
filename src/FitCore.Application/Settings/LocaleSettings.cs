namespace FitCore.Application.Settings;

public sealed record LocaleSettings : ISettingsGroup
{
    public static string GroupKey => "locale";

    public string CurrencyCode { get; init; } = "USD";
    public string CurrencySymbol { get; init; } = "$";
    public bool SymbolBeforeAmount { get; init; } = true;
    public int DecimalPlaces { get; init; } = 2;
    public string ThousandsSeparator { get; init; } = ",";
    public string DecimalSeparator { get; init; } = ".";
    public string TimeZoneId { get; init; } = "UTC";
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public string TimeFormat { get; init; } = "HH:mm";
    public int FirstDayOfWeek { get; init; } = 1; // Monday = 1 (.NET DayOfWeek)
}
