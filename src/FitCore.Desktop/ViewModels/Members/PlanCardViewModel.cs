using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Memberships;
using FitCore.Application.Plans;

namespace FitCore.Desktop.ViewModels.Members;

public sealed class PlanCardViewModel : ObservableObject
{
    private readonly ICurrencyFormatter _money;

    public int Id { get; }
    public string Name { get; }
    public string Category { get; }
    public long PriceMinor { get; }
    public string CurrencyCode { get; }
    public int DurationValue { get; }
    public string DurationUnit { get; }

    private string _priceDisplay = string.Empty;
    public string PriceDisplay
    {
        get => _priceDisplay;
        private set => SetProperty(ref _priceDisplay, value);
    }

    public string DurationDisplay => DurationUnit switch
    {
        "Days" => DurationValue == 1 ? "1 day" : $"{DurationValue} days",
        "Weeks" => DurationValue == 1 ? "1 week" : $"{DurationValue} weeks",
        "Months" => DurationValue == 1 ? "1 month" : $"{DurationValue} months",
        _ => $"{DurationValue} {DurationUnit}"
    };

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public PlanCardViewModel(MembershipPlanListItem item, ICurrencyFormatter money)
    {
        Id = item.Id;
        Name = item.Name;
        Category = item.Category;
        PriceMinor = item.PriceMinor;
        CurrencyCode = item.CurrencyCode;
        DurationValue = item.DurationValue;
        DurationUnit = item.DurationUnit;
        _money = money;
    }

    public async Task LoadDisplayAsync(CancellationToken ct = default)
    {
        PriceDisplay = await _money.FormatAsync(PriceMinor, CurrencyCode, ct);
    }
}

public sealed class PlanCategoryGroup : ObservableObject
{
    public string Category { get; }
    public string DisplayName { get; }
    public ObservableCollection<PlanCardViewModel> Plans { get; } = new();

    public PlanCategoryGroup(string category, string displayName)
    {
        Category = category;
        DisplayName = displayName;
    }
}
