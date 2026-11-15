namespace FitCore.Application.Modules;

/// <summary>Describes an optional module (PRS §9.2).</summary>
public sealed record ModuleDefinition(
    string Key,
    string DisplayName,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> NavigationKeys,
    bool AlwaysOn = false);
