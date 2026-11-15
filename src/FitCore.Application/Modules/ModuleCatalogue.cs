using FitCore.Application.Modules;

namespace FitCore.Application.Modules;

/// <summary>
/// The 7 optional modules from PRS §9.1.
/// Adding a module = add a ModuleDefinition here, not edit core code (§9.2).
/// </summary>
public static class ModuleCatalogue
{
    public static readonly IReadOnlyList<ModuleDefinition> All = new[]
    {
        new ModuleDefinition(
            Key: "trainers",
            DisplayName: "Trainer Management",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "trainers.manage" },
            NavigationKeys: new[] { "nav.trainers" }),

        new ModuleDefinition(
            Key: "programmes",
            DisplayName: "Workout Programmes",
            Dependencies: new[] { "trainers" },
            Permissions: new[] { "programmes.manage" },
            NavigationKeys: new[] { "nav.programmes" }),

        new ModuleDefinition(
            Key: "progress",
            DisplayName: "Member Progress Tracking",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "progress.record" },
            NavigationKeys: new[] { "nav.progress" }),

        new ModuleDefinition(
            Key: "inventory",
            DisplayName: "Inventory",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "inventory.manage" },
            NavigationKeys: new[] { "nav.inventory" }),

        new ModuleDefinition(
            Key: "expenses",
            DisplayName: "Expense Management",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "expenses.record" },
            NavigationKeys: new[] { "nav.expenses" }),

        new ModuleDefinition(
            Key: "retention",
            DisplayName: "Retention & Follow-ups",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "followups.manage" },
            NavigationKeys: new[] { "nav.retention" }),

        new ModuleDefinition(
            Key: "loyalty",
            DisplayName: "Referral & Loyalty",
            Dependencies: Array.Empty<string>(),
            Permissions: new[] { "loyalty.manage" },
            NavigationKeys: new[] { "nav.loyalty" }),
    };
}
