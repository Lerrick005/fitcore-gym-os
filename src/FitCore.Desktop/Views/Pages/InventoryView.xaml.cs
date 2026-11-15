using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.Views.Modules;
using FitCore.Application.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryViewModel vm)
        {
            vm.EditRequested += OnEditRequested;
            vm.AdjustStockRequested += OnAdjustStockRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryViewModel vm)
        {
            vm.EditRequested -= OnEditRequested;
            vm.AdjustStockRequested -= OnAdjustStockRequested;
        }
    }

    private async void OnEditRequested(object? sender, int? id)
    {
        if (sender is not InventoryViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<InventoryEditViewModel>();
        var window = new InventoryEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(id);
        window.ShowDialog();
    }

    private async void OnAdjustStockRequested(object? sender, int id)
    {
        if (sender is not InventoryViewModel vm) return;
        if (id <= 0) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var adjustVm = scope.ServiceProvider.GetRequiredService<StockAdjustmentViewModel>();
        var window = new StockAdjustmentWindow { DataContext = adjustVm, Owner = Window.GetWindow(this) };

        adjustVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await adjustVm.InitializeAsync(id);
        window.ShowDialog();
    }
}
