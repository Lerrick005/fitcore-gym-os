using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Settings;

namespace FitCore.Desktop.ViewModels.Wizard;

/// <summary>Editable wrapper for one PlanTemplate in wizard step 6.</summary>
public sealed partial class PlanTemplateViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private long _priceMinor;
    [ObservableProperty] private int _durationValue = 1;
    [ObservableProperty] private int _durationUnit;
    [ObservableProperty] private int? _freezeAllowanceDays;
    [ObservableProperty] private int? _visitLimit;
    [ObservableProperty] private long? _joiningFeeMinor;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private int _sortOrder;

    public PlanTemplateViewModel() { }

    public PlanTemplateViewModel(PlanTemplate t)
    {
        _name = t.Name;
        _description = t.Description;
        _priceMinor = t.PriceMinor;
        _durationValue = t.DurationValue;
        _durationUnit = t.DurationUnit;
        _freezeAllowanceDays = t.FreezeAllowanceDays;
        _visitLimit = t.VisitLimit;
        _joiningFeeMinor = t.JoiningFeeMinor;
        _isActive = t.IsActive;
        _sortOrder = t.SortOrder;
    }

    public PlanTemplate ToModel() => new()
    {
        Name = Name,
        Description = Description,
        PriceMinor = PriceMinor,
        DurationValue = DurationValue,
        DurationUnit = DurationUnit,
        FreezeAllowanceDays = FreezeAllowanceDays,
        VisitLimit = VisitLimit,
        JoiningFeeMinor = JoiningFeeMinor,
        IsActive = IsActive,
        SortOrder = SortOrder
    };
}
