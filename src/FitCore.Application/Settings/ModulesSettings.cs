namespace FitCore.Application.Settings;

public sealed record ModulesSettings : ISettingsGroup
{
    public static string GroupKey => "modules";

    // Enabled/disabled state lives in Modules table.
    // This group holds only user-preference bits that are not enablement.
    public bool ShowDisabledModulesInSettings { get; init; } = true;
    public bool AutoEnableDependenciesOnEnable { get; init; }
}
