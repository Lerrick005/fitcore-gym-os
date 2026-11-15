using System.Linq;
using FitCore.Infrastructure.Modules;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Modules;

public class ModuleRegistryTests
{
    private readonly ModuleRegistry _registry = new();

    [Fact]
    public void Exposes_Seven_Modules()
    {
        _registry.All.Count.ShouldBe(7);
        _registry.All.Select(m => m.Key).ShouldBe(
            new[] { "trainers", "programmes", "progress", "inventory", "expenses", "retention", "loyalty" },
            ignoreOrder: true);
    }

    [Fact]
    public void DependenciesOf_Programmes_Contains_Trainers()
    {
        var deps = _registry.DependenciesOf("programmes");
        deps.Count.ShouldBe(1);
        deps[0].Key.ShouldBe("trainers");
    }

    [Fact]
    public void DependentsOf_Trainers_Contains_Programmes()
    {
        var dependents = _registry.DependentsOf("trainers");
        dependents.Select(m => m.Key).ShouldContain("programmes");
    }
}
