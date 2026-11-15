namespace FitCore.Application.Settings;

public sealed record BrandingSettings : ISettingsGroup
{
    public static string GroupKey => "branding";

    public string BusinessName { get; init; } = string.Empty;
    public string TradingName { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string LogoLightPath { get; init; } = string.Empty;
    public string LogoDarkPath { get; init; } = string.Empty;
    public string PrimaryColor { get; init; } = "#3B82F6";
    public string SecondaryColor { get; init; } = "#6B7280";
    public string AccentColor { get; init; } = "#10B981";
}
