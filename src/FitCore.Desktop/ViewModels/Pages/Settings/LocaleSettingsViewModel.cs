using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Application.Setup.Validators;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class LocaleSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _currencyCode = "KES";
    [ObservableProperty] private string _currencySymbol = "KSh";
    [ObservableProperty] private bool _symbolBeforeAmount = true;
    [ObservableProperty] private int _decimalPlaces = 2;
    [ObservableProperty] private string _thousandsSeparator = ",";
    [ObservableProperty] private string _decimalSeparator = ".";
    [ObservableProperty] private string _timeZoneId = "Africa/Nairobi";
    [ObservableProperty] private string _dateFormat = "dd/MM/yyyy";
    [ObservableProperty] private string _timeFormat = "HH:mm";
    [ObservableProperty] private int _firstDayOfWeek;

    public override string GroupName => "Locale & formats";
    public override string GroupDescription => "Currency, time zone and how dates and times are displayed.";

    public LocaleSettingsViewModel(ISettingsService settings) => _settings = settings;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync<LocaleSettings>(ct);
        CurrencyCode = s.CurrencyCode;
        CurrencySymbol = s.CurrencySymbol;
        SymbolBeforeAmount = s.SymbolBeforeAmount;
        DecimalPlaces = s.DecimalPlaces;
        ThousandsSeparator = s.ThousandsSeparator;
        DecimalSeparator = s.DecimalSeparator;
        TimeZoneId = s.TimeZoneId;
        DateFormat = s.DateFormat;
        TimeFormat = s.TimeFormat;
        FirstDayOfWeek = s.FirstDayOfWeek;
    }

    protected override async Task<Result> SaveCoreAsync(CancellationToken ct)
    {
        var s = new LocaleSettings
        {
            CurrencyCode = CurrencyCode,
            CurrencySymbol = CurrencySymbol,
            SymbolBeforeAmount = SymbolBeforeAmount,
            DecimalPlaces = DecimalPlaces,
            ThousandsSeparator = ThousandsSeparator,
            DecimalSeparator = DecimalSeparator,
            TimeZoneId = TimeZoneId,
            DateFormat = DateFormat,
            TimeFormat = TimeFormat,
            FirstDayOfWeek = FirstDayOfWeek
        };
        var v = await new LocaleValidator().ValidateAsync(s, ct);
        if (!v.IsValid) return Result.Fail("settings.validation", string.Join(" ", v.Errors.ConvertAll(e => e.ErrorMessage)));
        return await _settings.SaveAsync(s, ct);
    }
}
