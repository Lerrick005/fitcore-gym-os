using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class ExpensesView : UserControl
{
    public ExpensesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpensesViewModel vm)
        {
            vm.AddExpenseRequested += OnAddExpenseRequested;
            vm.AddCategoryRequested += OnAddCategoryRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpensesViewModel vm)
        {
            vm.AddExpenseRequested -= OnAddExpenseRequested;
            vm.AddCategoryRequested -= OnAddCategoryRequested;
        }
    }

    private async void OnAddExpenseRequested(object? sender, EventArgs e)
    {
        if (sender is not ExpensesViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ExpenseEditViewModel>();
        var window = new ExpenseEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync();
        window.ShowDialog();
    }

    private async void OnAddCategoryRequested(object? sender, EventArgs e)
    {
        if (sender is not ExpensesViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ExpenseEditViewModel>();
        var window = new ExpenseEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync();
        window.ShowDialog();
    }
}
