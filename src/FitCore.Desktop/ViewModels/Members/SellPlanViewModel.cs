using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Formatting;
using FitCore.Application.Memberships;
using FitCore.Application.Plans;

namespace FitCore.Desktop.ViewModels.Members;

public sealed partial class SellPlanViewModel : ObservableObject
{
    private readonly IMembershipService _memberships;
    private readonly IMembershipPlanService _plans;
    private readonly ICurrencyFormatter _money;
    private int _memberId;

    public ObservableCollection<PlanCategoryGroup> Groups { get; } = new();

    [ObservableProperty] private PlanCardViewModel? _selectedPlan;
    [ObservableProperty] private DateTime? _startDate = DateTime.UtcNow.Date;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _previewEndDate = string.Empty;

    public event EventHandler<bool>? CloseRequested;

    public SellPlanViewModel(IMembershipService memberships, IMembershipPlanService plans, ICurrencyFormatter money)
    {
        _memberships = memberships;
        _plans = plans;
        _money = money;
    }

    public async Task InitializeAsync(int memberId, CancellationToken ct = default)
    {
        _memberId = memberId;
        var result = await _plans.GetActiveAsync(ct);

        Groups.Clear();
        if (!result.IsSuccess) return;

        var grouped = result.Value!
            .GroupBy(p => p.Category)
            .OrderBy(g => CategoryOrder(g.Key));

        foreach (var g in grouped)
        {
            var catGroup = new PlanCategoryGroup(g.Key, CategoryDisplay(g.Key));
            foreach (var p in g)
            {
                var card = new PlanCardViewModel(p, _money);
                await card.LoadDisplayAsync(ct);
                catGroup.Plans.Add(card);
            }
            Groups.Add(catGroup);
        }
    }

    private static int CategoryOrder(string cat) => cat switch
    {
        "Membership" => 0,
        "Pass" => 1,
        "Service" => 2,
        _ => 3
    };

    private static string CategoryDisplay(string cat) => cat switch
    {
        "Membership" => "MEMBERSHIPS",
        "Pass" => "PASSES",
        "Service" => "SERVICES",
        _ => cat.ToUpperInvariant()
    };

    [RelayCommand]
    private void SelectPlan(PlanCardViewModel? plan)
    {
        if (plan is null) return;

        // Deselect all
        foreach (var g in Groups)
            foreach (var p in g.Plans)
                p.IsSelected = false;

        plan.IsSelected = true;
        SelectedPlan = plan;
        UpdatePreview();
    }

    partial void OnStartDateChanged(DateTime? value) => UpdatePreview();

    private void UpdatePreview()
    {
        if (SelectedPlan is null || StartDate is null)
        {
            PreviewEndDate = string.Empty;
            return;
        }

        var start = StartDate.Value.Date;
        var end = SelectedPlan.DurationUnit switch
        {
            "Days" => start.AddDays(SelectedPlan.DurationValue),
            "Weeks" => start.AddDays(SelectedPlan.DurationValue * 7),
            "Months" => start.AddMonths(SelectedPlan.DurationValue),
            _ => start
        };
        PreviewEndDate = $"Ends {end:dd/MM/yyyy}";
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (SelectedPlan is null)
        {
            ErrorMessage = "Select a plan.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var request = new SellMembershipRequest(_memberId, SelectedPlan.Id, StartDate?.Date, null);
            var result = await _memberships.SellAsync(request);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
