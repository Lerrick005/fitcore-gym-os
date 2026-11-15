namespace FitCore.Application.Modules;

/// <summary>Module state as returned to the UI (not to be confused with the DB entity FitCore.Domain.Entities.ModuleState).</summary>
public sealed record ModuleStateDto(string Key, string DisplayName, bool IsEnabled, IReadOnlyList<string> Dependencies);
