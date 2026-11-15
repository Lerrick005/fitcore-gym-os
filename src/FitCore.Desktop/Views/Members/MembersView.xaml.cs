using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Application.Members;
using FitCore.Desktop.ViewModels;
using FitCore.Desktop.ViewModels.Members;
using FitCore.Desktop.Views.Members;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Members;

public partial class MembersView : UserControl
{
    public MembersView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MembersViewModel vm)
        {
            vm.EditRequested += OnEditRequested;
            vm.AddMemberFlowRequested += OnAddMemberFlow;
            vm.QuickSaleRequested += OnQuickSale;
            vm.ImportRequested += OnImport;
        }
    }

    private async void OnQuickSale(object? sender, EventArgs e)
    {
        if (sender is not MembersViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(sp);
        var quickVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<QuickSaleViewModel>(scope.ServiceProvider);
        var window = new QuickSaleWindow { DataContext = quickVm, Owner = Window.GetWindow(this) };

        quickVm.Completed += async (_, paymentId) =>
        {
            window.Close();
            if (paymentId > 0)
            {
                await vm.RefreshAsync();
                try
                {
                    var receiptVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ReceiptViewModel>(scope.ServiceProvider);
                    var receiptWindow = new ReceiptWindow { DataContext = receiptVm, Owner = Window.GetWindow(this) };
                    await receiptVm.InitializeForPaymentAsync(paymentId);
                    receiptWindow.ShowDialog();
                }
                catch { /* non-fatal */ }
            }
        };

        await quickVm.InitializeAsync();
        window.ShowDialog();
    }

    private async void OnImport(object? sender, EventArgs e)
    {
        if (sender is not MembersViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(sp);
        var importVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MemberImportViewModel>(scope.ServiceProvider);
        var window = new MemberImportWindow { DataContext = importVm, Owner = Window.GetWindow(this) };

        importVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        window.ShowDialog();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MembersViewModel vm)
        {
            vm.EditRequested -= OnEditRequested;
            vm.AddMemberFlowRequested -= OnAddMemberFlow;
            vm.QuickSaleRequested -= OnQuickSale;
            vm.ImportRequested -= OnImport;
        }
    }

    private async void OnAddMemberFlow(object? sender, EventArgs e)
    {
        if (sender is not MembersViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(sp);
        var flowVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<NewMemberFlowViewModel>(scope.ServiceProvider);
        var window = new NewMemberFlowWindow { DataContext = flowVm, Owner = Window.GetWindow(this) };

        flowVm.MemberCreated += async (_, memberId) =>
        {
            window.Close();
            if (memberId > 0)
                await vm.RefreshAsync();
        };

        await flowVm.InitializeAsync();
        window.ShowDialog();
    }

    private async void OnEditRequested(object? sender, MemberListItem? member)
    {
        if (sender is not MembersViewModel vm) return;

        var app = (App)System.Windows.Application.Current;
        var scopeFactory = app.Services?.GetService<IServiceScopeFactory>();
        if (scopeFactory is null) return;

        using var scope = scopeFactory.CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<MemberEditViewModel>();

        var window = new MemberEditWindow
        {
            DataContext = editVm,
            Owner = Window.GetWindow(this)
        };

        editVm.CloseRequested += (_, ok) =>
        {
            if (ok) _ = vm.RefreshAsync();
            window.Close();
        };

        await editVm.InitializeAsync(member?.Id);
        window.ShowDialog();
    }
}




