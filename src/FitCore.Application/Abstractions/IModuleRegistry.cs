using FitCore.Application.Modules;

namespace FitCore.Application.Abstractions;

/// <summary>Central module registry (PRS §9.2). Adding a module means registering it here.</summary>
public interface IModuleRegistry
{
    IReadOnlyList<ModuleDefinition> All { get; }
    ModuleDefinition? Find(string key);
    IReadOnlyList<ModuleDefinition> DependenciesOf(string key);
    IReadOnlyList<ModuleDefinition> DependentsOf(string key);
}
