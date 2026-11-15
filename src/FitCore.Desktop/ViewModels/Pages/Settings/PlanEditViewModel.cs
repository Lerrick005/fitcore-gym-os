using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Plans;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class PlanEditViewModel : ObservableObject
{
    private readonly IMembershipPlanService _plans;
    private int? _planId;

    [ObservableProperty] private string _title = "Add plan";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _priceInput = string.Empty;
    [ObservableProperty] private int _durationValue = 1;
    [ObservableProperty] private string _durationUnit = "Months";
    [ObservableProperty] private string _category = "Membership";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] DurationUnits { get; } = new[] { "Days", "Weeks", "Months" };
    public string[] Categories { get; } = new[] { "Membership", "Pass", "Service" };

    public event EventHandler<bool>? CloseRequested;

    public PlanEditViewModel(IMembershipPlanService plans) => _plans = plans;

    public async Task InitializeAsync(int? planId, CancellationToken ct = default)
    {
        _planId = planId;
        if (planId is null) { Title = "Add plan"; return; }

        Title = "Edit plan";
        var r = await _plans.GetAsync(planId.Value, ct);
        if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
        var p = r.Value!;
        Name = p.Name;
        Description = p.Description ?? string.Empty;
        PriceInput = (p.PriceMinor / 100m).ToString("0.00");
        DurationValue = p.DurationValue;
        DurationUnit = p.DurationUnit.ToString();
        Category = p.Category;
        SortOrder = p.SortOrder;
        IsActive = p.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (!decimal.TryParse(PriceInput, out var price) || price < 0)
        {
            ErrorMessage = "Price must be a valid amount.";
            return;
        }

        IsBusy = true;
        try
        {
            var input = new PlanInput(
                Name, string.IsNullOrWhiteSpace(Description) ? null : Description,
                (long)(price * 100m), DurationValue, DurationUnit, Category, SortOrder, IsActive);

            var result = _planId is null
                ? await _plans.CreateAsync(input)
                : await _plans.UpdateAsync(_planId.Value, input);

            if (!result.IsSuccess) { ErrorMessage = result.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
