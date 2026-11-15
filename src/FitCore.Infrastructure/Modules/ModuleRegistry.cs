using FitCore.Application.Abstractions;
using FitCore.Application.Modules;

namespace FitCore.Infrastructure.Modules;

/// <summary>
/// Validates and exposes the module catalogue (PRS §9.2). Fail-fast on bad config.
/// </summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IReadOnlyList<ModuleDefinition> _all;
    private readonly Dictionary<string, ModuleDefinition> _byKey;

    public ModuleRegistry()
    {
        _all = ModuleCatalogue.All;

        // Fail-fast validation
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in _all)
        {
            if (!keys.Add(m.Key))
                throw new InvalidOperationException($"Duplicate module key: '{m.Key}'.");
        }

        _byKey = _all.ToDictionary(m => m.Key, StringComparer.Ordinal);

        foreach (var m in _all)
        {
            foreach (var dep in m.Dependencies)
            {
                if (!_byKey.ContainsKey(dep))
                    throw new InvalidOperationException(
                        $"Module '{m.Key}' declares dependency '{dep}' which is not in the catalogue.");
            }
        }

        // Cycle detection (DFS three-colour)
        var visited = new Dictionary<string, int>(StringComparer.Ordinal); // 0=unvisited, 1=in-stack, 2=done
        foreach (var m in _all) Visit(m.Key);

        void Visit(string key)
        {
            if (!visited.TryGetValue(key, out var state) || state == 0)
            {
                visited[key] = 1;
                foreach (var dep in _byKey[key].Dependencies) Visit(dep);
                visited[key] = 2;
            }
            else if (state == 1)
            {
                throw new InvalidOperationException($"Module dependency cycle detected involving '{key}'.");
            }
        }
    }

    public IReadOnlyList<ModuleDefinition> All => _all;

    public ModuleDefinition? Find(string key) => _byKey.TryGetValue(key, out var m) ? m : null;

    public IReadOnlyList<ModuleDefinition> DependenciesOf(string key)
    {
        if (!_byKey.TryGetValue(key, out var m)) return Array.Empty<ModuleDefinition>();
        return m.Dependencies
            .Select(k => _byKey[k])
            .ToArray();
    }

    public IReadOnlyList<ModuleDefinition> DependentsOf(string key)
    {
        return _all
            .Where(m => m.Dependencies.Contains(key, StringComparer.Ordinal))
            .ToArray();
    }
}
