using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Application.Retention;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class RetentionView : UserControl
{
    public RetentionView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RetentionViewModel vm)
        {
            vm.CreateFollowUpRequested += OnCreateFollowUp;
            vm.CompleteFollowUpRequested += OnCompleteFollowUp;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RetentionViewModel vm)
        {
            vm.CreateFollowUpRequested -= OnCreateFollowUp;
            vm.CompleteFollowUpRequested -= OnCompleteFollowUp;
        }
    }

    private async void OnCreateFollowUp(object? sender, AtRiskMember row)
    {
        if (sender is not RetentionViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var retention = scope.ServiceProvider.GetRequiredService<IRetentionService>();

        var dueAt = DateTime.UtcNow.AddDays(3);
        var input = new FollowUpInput(row.MemberId, dueAt, null);
        await retention.CreateFollowUpAsync(input);

        await vm.RefreshAsync();
    }

    private async void OnCompleteFollowUp(object? sender, FollowUpItem row)
    {
        if (sender is not RetentionViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<FollowUpCompleteViewModel>();
        var window = new FollowUpCompleteWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (!ok) return;
            var retention = scope.ServiceProvider.GetRequiredService<IRetentionService>();
            await retention.CompleteFollowUpAsync(row.Id, editVm.OutcomeValue);
            await vm.RefreshAsync();
        };

        editVm.Initialize(row.Id, row.MemberName);
        window.ShowDialog();
    }
}
