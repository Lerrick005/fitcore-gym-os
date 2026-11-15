using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Inventory;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class InventoryViewModel : PageViewModelBase
{
    private readonly IInventoryService _inventory;

    public ObservableCollection<InventoryItemRow> Rows { get; } = new();
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showInactive;
    [ObservableProperty] private bool _showLowStock;

    public override string Title => "Inventory";

    public event EventHandler<int?>? EditRequested;
    public event EventHandler<int>? AdjustStockRequested;

    public InventoryViewModel(IInventoryService inventory) => _inventory = inventory;

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var r = ShowLowStock
                ? await _inventory.ListLowStockAsync(ct)
                : await _inventory.ListAsync(ShowInactive, ct);
            Rows.Clear();
            if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
            foreach (var i in r.Value!) Rows.Add(i);
        }
        finally { IsBusy = false; }
    }

    partial void OnShowInactiveChanged(bool value) => _ = RefreshAsync();
    partial void OnShowLowStockChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private void AddItem() => EditRequested?.Invoke(this, null);

    [RelayCommand]
    private void EditItem(InventoryItemRow? row) => EditRequested?.Invoke(this, row?.Id);

    [RelayCommand]
    private void AdjustStock(InventoryItemRow? row) => AdjustStockRequested?.Invoke(this, row?.Id ?? 0);
}
