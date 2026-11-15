using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Settings;

namespace FitCore.Desktop.ViewModels.Wizard;

public sealed partial class ReviewViewModel : WizardStepViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IDefaultsProvider _defaults;

    [ObservableProperty] private string _summaryBusiness = string.Empty;
    [ObservableProperty] private string _summaryCurrency = string.Empty;
    [ObservableProperty] private string _summaryTimeZone = string.Empty;
    [ObservableProperty] private string _summaryPlans = string.Empty;

    public override string Title => "Ready to go";
    public override string Description => "Click Finish to complete setup and start using FitCore.";

    public ReviewViewModel(ISettingsService settings, IDefaultsProvider defaults)
    {
        _settings = settings;
        _defaults = defaults;
    }

    protected override async Task LoadAsyncCore(CancellationToken ct)
    {
        var b = await _settings.GetAsync<BrandingSettings>(ct);
        var l = await _settings.GetAsync<LocaleSettings>(ct);

        SummaryBusiness = string.IsNullOrWhiteSpace(b.BusinessName) ? "(no name set)" : b.BusinessName;

        // If locale is still at model default (USD/UTC) but Kenya defaults exist,
        // show what WILL be applied at finish.
        if (l.CurrencyCode == "USD" && l.TimeZoneId == "UTC")
        {
            var localeDefaults = await _defaults.GetLocaleDefaultsAsync(ct);
            SummaryCurrency = $"{localeDefaults.CurrencyCode} ({localeDefaults.CurrencySymbol})";
            SummaryTimeZone = localeDefaults.TimeZoneId;
        }
        else
        {
            SummaryCurrency = $"{l.CurrencyCode} ({l.CurrencySymbol})";
            SummaryTimeZone = l.TimeZoneId;
        }

        SummaryPlans = "10 Body Craft plans will be ready on the dashboard.";
    }

    protected override Task<string?> ValidateAsyncCore(CancellationToken ct) => Task.FromResult<string?>(null);

    protected override Task<FitCore.Application.Common.Result> SaveAsyncCore(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
