using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Application.Setup.Validators;
using FitCore.Application.Theming;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class BrandingSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;

    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _tradingName = string.Empty;
    [ObservableProperty] private string _tagline = string.Empty;
    [ObservableProperty] private string _primaryColor = "#B8451F";
    [ObservableProperty] private string _secondaryColor = "#6B7280";
    [ObservableProperty] private string _accentColor = "#15803D";
    [ObservableProperty] private string _logoLightPath = string.Empty;
    [ObservableProperty] private string _logoDarkPath = string.Empty;
    [ObservableProperty] private string _contrastWarning = string.Empty;
    [ObservableProperty] private bool _hasContrastWarning;

    public override string GroupName => "Branding";
    public override string GroupDescription => "How your gym appears in the app and on receipts.";

    public BrandingSettingsViewModel(ISettingsService settings, IThemeService theme)
    {
        _settings = settings;
        _theme = theme;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync<BrandingSettings>(ct);
        BusinessName = s.BusinessName;
        TradingName = s.TradingName;
        Tagline = s.Tagline;
        PrimaryColor = s.PrimaryColor;
        SecondaryColor = s.SecondaryColor;
        AccentColor = s.AccentColor;
        LogoLightPath = s.LogoLightPath;
        LogoDarkPath = s.LogoDarkPath;
        CheckContrast();
    }

    // Live preview: when PrimaryColor changes, apply immediately (no save needed)
    partial void OnPrimaryColorChanged(string value)
    {
        CheckContrast();
        if (BrandColorCalculator.IsValidHex(value))
            _theme.ApplyBrandColor(value);
    }

    private void CheckContrast()
    {
        if (!BrandColorCalculator.IsValidHex(PrimaryColor))
        {
            ContrastWarning = "Color must be a #RRGGBB hex value.";
            HasContrastWarning = true;
            return;
        }

        var onPrimary = BrandColorCalculator.PickReadableForeground(PrimaryColor);
        var ratio = BrandColorCalculator.ContrastRatio(PrimaryColor, onPrimary);
        if (ratio < 4.5)
        {
            ContrastWarning = $"Contrast is low ({ratio:F1}:1). Text may be hard to read.";
            HasContrastWarning = true;
        }
        else
        {
            ContrastWarning = string.Empty;
            HasContrastWarning = false;
        }
    }

    protected override async Task<Result> SaveCoreAsync(CancellationToken ct)
    {
        var s = new BrandingSettings
        {
            BusinessName = BusinessName,
            TradingName = TradingName,
            Tagline = Tagline,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            AccentColor = AccentColor,
            LogoLightPath = LogoLightPath,
            LogoDarkPath = LogoDarkPath
        };

        var v = await new BrandingValidator().ValidateAsync(s, ct);
        if (!v.IsValid) return Result.Fail("settings.validation", string.Join(" ", v.Errors.ConvertAll(e => e.ErrorMessage)));

        var r = await _settings.SaveAsync(s, ct);
        if (r.IsSuccess)
            _theme.ApplyBrandColor(s.PrimaryColor);
        return r;
    }
}
