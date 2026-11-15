using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Inventory;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class StockAdjustmentViewModel : ObservableObject
{
    private readonly IInventoryService _inventory;
    private int _itemId;

    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private string _currentStock = "0";
    [ObservableProperty] private string _delta = "1";
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public StockAdjustmentViewModel(IInventoryService inventory) => _inventory = inventory;

    public async Task InitializeAsync(int itemId, CancellationToken ct = default)
    {
        _itemId = itemId;
        var r = await _inventory.ListAsync(true, ct);
        if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
        var item = System.Linq.Enumerable.FirstOrDefault(r.Value!, i => i.Id == itemId);
        if (item is null) { ErrorMessage = "Item not found."; return; }
        ItemName = item.Name;
        CurrentStock = item.StockLevel.ToString();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (!int.TryParse(Delta, out var delta) || delta == 0) { ErrorMessage = "Enter a non-zero change (positive to add, negative to remove)."; return; }

        IsBusy = true;
        try
        {
            var r = await _inventory.AdjustStockAsync(new StockAdjustment(_itemId, delta, string.IsNullOrWhiteSpace(Reason) ? null : Reason));
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
