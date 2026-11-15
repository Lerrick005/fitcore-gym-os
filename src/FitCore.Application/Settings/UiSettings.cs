namespace FitCore.Application.Settings;

public sealed record UiSettings : ISettingsGroup
{
    public static string GroupKey => "ui";

    public string Theme { get; init; } = "light";
    public bool SidebarCollapsedByDefault { get; init; }
    public int AutoLockMinutes { get; init; } = 15;
    public string Language { get; init; } = "en";
}
