using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Formatting;
using FitCore.Application.Plans;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class PlanItemViewModel : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string PriceDisplay { get; }
    public string DurationDisplay { get; }
    public bool IsActive { get; }
    public long PriceMinor { get; }
    public string DurationUnit { get; }
    public int DurationValue { get; }

    public PlanItemViewModel(MembershipPlanListItem item, string priceDisplay)
    {
        Id = item.Id;
        Name = item.Name;
        Category = item.Category;
        PriceDisplay = priceDisplay;
        DurationDisplay = item.DurationUnit switch
        {
            "Days" => item.DurationValue == 1 ? "1 day" : $"{item.DurationValue} days",
            "Weeks" => item.DurationValue == 1 ? "1 week" : $"{item.DurationValue} weeks",
            "Months" => item.DurationValue == 1 ? "1 month" : $"{item.DurationValue} months",
            _ => $"{item.DurationValue} {item.DurationUnit}"
        };
        PriceMinor = item.PriceMinor;
        DurationUnit = item.DurationUnit;
        DurationValue = item.DurationValue;
    }
}

public sealed partial class PlanCategoryGroupVm
{
    public string DisplayName { get; }
    public ObservableCollection<PlanItemViewModel> Plans { get; } = new();
    public PlanCategoryGroupVm(string displayName) => DisplayName = displayName;
}

public sealed partial class PlansSettingsViewModel : SettingsPageViewModelBase
{
    private readonly IMembershipPlanService _plans;
    private readonly ICurrencyFormatter _money;

    public ObservableCollection<PlanCategoryGroupVm> Groups { get; } = new();

    public override string GroupName => "Plans";
    public override string GroupDescription => "The plans and services you sell to members.";

    public event EventHandler<int?>? EditRequested;
    public event EventHandler<int>? ArchiveRequested;
    public event EventHandler<int>? RestoreRequested;

    public PlansSettingsViewModel(IMembershipPlanService plans, ICurrencyFormatter money)
    {
        _plans = plans;
        _money = money;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var result = await _plans.GetAllAsync(ct);
        Groups.Clear();
        if (!result.IsSuccess) { StatusMessage = result.ErrorMessage; return; }

        var grouped = result.Value!.GroupBy(p => p.Category).OrderBy(g => CategoryOrder(g.Key));
        foreach (var g in grouped)
        {
            var group = new PlanCategoryGroupVm(CategoryDisplay(g.Key));
            foreach (var p in g)
            {
                var price = await _money.FormatAsync(p.PriceMinor, p.CurrencyCode, ct);
                group.Plans.Add(new PlanItemViewModel(p, price));
            }
            Groups.Add(group);
        }
    }

    private static int CategoryOrder(string cat) => cat switch { "Membership" => 0, "Pass" => 1, "Service" => 2, _ => 3 };
    private static string CategoryDisplay(string cat) => cat switch { "Membership" => "MEMBERSHIPS", "Pass" => "PASSES", "Service" => "SERVICES", _ => cat.ToUpperInvariant() };

    [RelayCommand]
    private void AddNew() => EditRequested?.Invoke(this, null);

    [RelayCommand]
    private void EditPlan(PlanItemViewModel? plan)
    {
        if (plan is null) return;
        EditRequested?.Invoke(this, plan.Id);
    }

    [RelayCommand]
    private void ArchivePlan(PlanItemViewModel? plan)
    {
        if (plan is null) return;
        ArchiveRequested?.Invoke(this, plan.Id);
    }

    [RelayCommand]
    private void RestorePlan(PlanItemViewModel? plan)
    {
        if (plan is null) return;
        RestoreRequested?.Invoke(this, plan.Id);
    }

    protected override Task<FitCore.Application.Common.Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
