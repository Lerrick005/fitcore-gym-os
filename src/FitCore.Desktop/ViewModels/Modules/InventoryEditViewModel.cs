using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Inventory;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class InventoryEditViewModel : ObservableObject
{
    private readonly IInventoryService _inventory;
    private int? _itemId;

    [ObservableProperty] private string _title = "Add item";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _sku = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private string _customCategory = string.Empty;
    public System.Collections.ObjectModel.ObservableCollection<string> Categories { get; } = new()
    {
        "Free Weights",
        "Machines",
        "Accessories",
        "Supplements",
        "Merchandise",
        "Beverages",
        "Other"
    };
    [ObservableProperty] private string _stockLevel = "0";
    [ObservableProperty] private string _lowStockThreshold = "5";
    [ObservableProperty] private string _unitPrice = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public InventoryEditViewModel(IInventoryService inventory) => _inventory = inventory;

    public async Task InitializeAsync(int? itemId, CancellationToken ct = default)
    {
        _itemId = itemId;
        Title = itemId.HasValue ? "Edit item" : "Add item";
        if (!itemId.HasValue) return;

        var r = await _inventory.ListAsync(true, ct);
        if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
        var item = System.Linq.Enumerable.FirstOrDefault(r.Value!, i => i.Id == itemId.Value);
        if (item is null) { ErrorMessage = "Item not found."; return; }

        Name = item.Name;
        Sku = item.Sku ?? string.Empty;
        Category = item.Category ?? string.Empty;
        SelectedCategory = !string.IsNullOrEmpty(item.Category) && Categories.Contains(item.Category) ? item.Category : "Other";
        StockLevel = item.StockLevel.ToString();
        LowStockThreshold = item.LowStockThreshold.ToString();
        UnitPrice = item.UnitPriceMinor.HasValue ? (item.UnitPriceMinor.Value / 100m).ToString("0.00") : string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        if (!int.TryParse(StockLevel, out var stock)) { ErrorMessage = "Stock level must be a number."; return; }
        if (!int.TryParse(LowStockThreshold, out var lowStock)) { ErrorMessage = "Low stock threshold must be a number."; return; }

        long? priceMinor = null;
        if (!string.IsNullOrWhiteSpace(UnitPrice) && decimal.TryParse(UnitPrice, out var price))
            priceMinor = (long)(price * 100m);

        IsBusy = true;
        try
        {
            var input = new InventoryItemInput(
                Name.Trim(),
                string.IsNullOrWhiteSpace(Sku) ? null : Sku.Trim(),
                string.IsNullOrWhiteSpace(SelectedCategory) || SelectedCategory == "Other" 
                ? (string.IsNullOrWhiteSpace(CustomCategory) ? "Other" : CustomCategory.Trim())
                : SelectedCategory,
                stock, lowStock, priceMinor,
                priceMinor.HasValue ? "KES" : null);

            var r = _itemId.HasValue
                ? await _inventory.UpdateAsync(_itemId.Value, input)
                : await _inventory.CreateAsync(input);

            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}


