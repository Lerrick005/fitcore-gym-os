using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.Views.Modules;
using FitCore.Application.Training;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class TrainersView : UserControl
{
    public TrainersView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TrainersViewModel vm)
        {
            vm.EditRequested += OnEditRequested;
            vm.ToggleRequested += OnToggleRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TrainersViewModel vm)
        {
            vm.EditRequested -= OnEditRequested;
            vm.ToggleRequested -= OnToggleRequested;
        }
    }

    private async void OnEditRequested(object? sender, int? id)
    {
        if (sender is not TrainersViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<TrainerEditViewModel>();
        var window = new TrainerEditWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(id);
        window.ShowDialog();
    }

    private async void OnToggleRequested(object? sender, int id)
    {
        if (sender is not TrainersViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITrainerService>();
        var r = await svc.GetAsync(id);
        if (!r.IsSuccess) { vm.StatusMessage = r.ErrorMessage; return; }

        await svc.SetActiveAsync(id, !r.Value!.IsActive);
        await vm.RefreshAsync();
    }
}
