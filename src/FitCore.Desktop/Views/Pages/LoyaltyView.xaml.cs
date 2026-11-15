using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.Views.Modules;
using FitCore.Application.Loyalty;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class LoyaltyView : UserControl
{
    public LoyaltyView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoyaltyViewModel vm)
        {
            vm.CreateReferralRequested += OnCreateReferral;
            vm.AddEntryRequested += OnAddEntry;
            vm.AddRewardRequested += OnAddReward;
            vm.EditRewardRequested += OnEditReward;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoyaltyViewModel vm)
        {
            vm.CreateReferralRequested -= OnCreateReferral;
            vm.AddEntryRequested -= OnAddEntry;
            vm.AddRewardRequested -= OnAddReward;
            vm.EditRewardRequested -= OnEditReward;
        }
    }

    private async void OnCreateReferral(object? sender, EventArgs e)
    {
        if (sender is not LoyaltyViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ReferralCreateViewModel>();
        var window = new ReferralCreateWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync();
        window.ShowDialog();
    }

    private async void OnAddEntry(object? sender, EventArgs e)
    {
        if (sender is not LoyaltyViewModel vm) return;
        var sp = ((System.Windows.Application.Current) as App)?.Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<LoyaltyEntryAddViewModel>();
        var window = new LoyaltyEntryAddWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync();
        window.ShowDialog();
    }

    private async void OnAddReward(object? sender, EventArgs e)
    {
        if (sender is not LoyaltyViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<RewardEditViewModel>();
        var window = new RewardEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(null);
        window.ShowDialog();
    }

    private async void OnEditReward(object? sender, int? rewardId)
    {
        if (sender is not LoyaltyViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<RewardEditViewModel>();
        var window = new RewardEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(rewardId);
        window.ShowDialog();
    }
}
