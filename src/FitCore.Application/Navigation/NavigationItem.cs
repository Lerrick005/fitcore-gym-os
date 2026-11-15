namespace FitCore.Application.Navigation;

/// <summary>
/// One entry in the sidebar. Visibility is determined by permission + module state.
/// Group controls which section header it appears under.
/// </summary>
public sealed record NavigationItem(
    string Key,
    string DisplayName,
    string IconGlyph,
    string? RequiredPermission,
    string? RequiredModule,
    string Group);
