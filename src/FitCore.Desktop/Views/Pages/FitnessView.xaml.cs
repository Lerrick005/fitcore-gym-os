using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class FitnessView : UserControl
{
    public FitnessView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FitnessViewModel vm)
        {
            vm.LogWorkoutRequested += OnLogWorkout;
            vm.LogBodyMetricRequested += OnLogBodyMetric;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FitnessViewModel vm)
        {
            vm.LogWorkoutRequested -= OnLogWorkout;
            vm.LogBodyMetricRequested -= OnLogBodyMetric;
        }
    }

    private async void OnLogWorkout(object? sender, EventArgs e)
    {
        if (sender is not FitnessViewModel vm) return;
        if (vm.SelectedMember is null)
        {
            MessageBox.Show("Select a member first.", "No member selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<LogWorkoutViewModel>();
        editVm.Initialize(vm.SelectedMember.MemberId);
        var window = new LogWorkoutWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok)
            {
                await vm.LoadAsync();
                // Reload selected member's detail
                if (vm.SelectedMember is not null) vm.SelectedMember = vm.Members.FirstOrDefault(m => m.MemberId == vm.SelectedMember.MemberId);
            }
        };

        window.ShowDialog();
    }

    private async void OnLogBodyMetric(object? sender, EventArgs e)
    {
        if (sender is not FitnessViewModel vm) return;
        if (vm.SelectedMember is null)
        {
            MessageBox.Show("Select a member first.", "No member selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<LogBodyMetricViewModel>();
        editVm.Initialize(vm.SelectedMember.MemberId);
        var window = new LogBodyMetricWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok)
            {
                await vm.LoadAsync();
                if (vm.SelectedMember is not null) vm.SelectedMember = vm.Members.FirstOrDefault(m => m.MemberId == vm.SelectedMember.MemberId);
            }
        };

        window.ShowDialog();
    }
}
