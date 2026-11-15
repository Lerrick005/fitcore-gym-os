namespace FitCore.Application.Theming;

/// <summary>
/// Runtime brand theme generation (PRS §7). Reads BrandingSettings, produces
/// brushes, and raises an event when branding changes so the UI re-themes live.
/// </summary>
public interface IThemeService
{
    /// <summary>Load current branding and produce brand brushes.</summary>
    Task ApplyFromSettingsAsync(CancellationToken ct = default);

    /// <summary>Directly apply a brand color (hex). Used by the branding preview in the wizard.</summary>
    void ApplyBrandColor(string primaryHex);

    /// <summary>Raised after brushes are updated so views can refresh if needed.</summary>
    event EventHandler? ThemeChanged;

    /// <summary>Current effective primary color (hex, uppercase).</summary>
    string CurrentPrimaryHex { get; }

    /// <summary>Contrast-safe foreground for the current primary color (hex).</summary>
    string CurrentOnPrimaryHex { get; }
}
