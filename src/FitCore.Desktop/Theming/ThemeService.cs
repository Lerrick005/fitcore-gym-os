using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using FitCore.Application.Abstractions;
using FitCore.Application.Settings;
using FitCore.Application.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Theming;

/// <summary>
/// WPF-specific brand theming service (PRS §7). Lives in Desktop because it manipulates WPF resources.
/// Uses IServiceScopeFactory to get a fresh ISettingsService per operation.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public event EventHandler? ThemeChanged;

    public string CurrentPrimaryHex { get; private set; } = "#B8451F";
    public string CurrentOnPrimaryHex { get; private set; } = "#FAF7F1";

    public ThemeService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        // Re-apply on branding change; subscribe once via a long-lived settings instance
        // created from a dedicated long-lived scope.
        var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        settings.Changed += async (_, args) =>
        {
            if (args.GroupKey == BrandingSettings.GroupKey)
                await ApplyFromSettingsAsync();
        };
    }

    public async Task ApplyFromSettingsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var branding = await settings.GetAsync<BrandingSettings>(ct);
        ApplyBrandColor(string.IsNullOrWhiteSpace(branding.PrimaryColor) ? "#B8451F" : branding.PrimaryColor);
    }

    public void ApplyBrandColor(string primaryHex)
    {
        if (!BrandColorCalculator.IsValidHex(primaryHex))
            primaryHex = "#B8451F";

        var primaryUpper = primaryHex.ToUpperInvariant();
        var hover = BrandColorCalculator.Darken(primaryUpper, 0.10);
        var pressed = BrandColorCalculator.Darken(primaryUpper, 0.20);
        var onPrimary = BrandColorCalculator.PickReadableForeground(primaryUpper);

        CurrentPrimaryHex = primaryUpper;
        CurrentOnPrimaryHex = onPrimary;

        var app = System.Windows.Application.Current;
        if (app is null) return;

        app.Dispatcher.Invoke(() =>
        {
            var resources = app.Resources;
            resources["Color.Brand.Primary"] = (Color)ColorConverter.ConvertFromString(primaryUpper);
            resources["Color.Brand.PrimaryHover"] = (Color)ColorConverter.ConvertFromString(hover);
            resources["Color.Brand.PrimaryPressed"] = (Color)ColorConverter.ConvertFromString(pressed);
            resources["Color.Text.Inverse"] = (Color)ColorConverter.ConvertFromString(onPrimary);

            resources["Brush.Brand.Primary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(primaryUpper));
            resources["Brush.Brand.PrimaryHover"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hover));
            resources["Brush.Brand.PrimaryPressed"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pressed));
            resources["Brush.Text.Inverse"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(onPrimary));

            ThemeChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}

