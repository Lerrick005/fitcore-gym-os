using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages.Settings;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages.Settings;

public partial class RewardsSettingsView : UserControl
{
    public RewardsSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RewardsSettingsViewModel vm)
            vm.EditRequested += OnEditRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RewardsSettingsViewModel vm)
            vm.EditRequested -= OnEditRequested;
    }

    private async void OnEditRequested(object? sender, int? id)
    {
        if (sender is not RewardsSettingsViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<RewardEditViewModel>();
        var window = new RewardEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.LoadAsync();
        };

        await editVm.InitializeAsync(id);
        window.ShowDialog();
    }
}
