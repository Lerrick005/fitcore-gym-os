using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class ClassesView : UserControl
{
    public ClassesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClassesViewModel vm)
        {
            vm.AddClassRequested += OnAddClass;
            vm.BookMemberRequested += OnBookMember;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClassesViewModel vm)
        {
            vm.AddClassRequested -= OnAddClass;
            vm.BookMemberRequested -= OnBookMember;
        }
    }

    private async void OnAddClass(object? sender, EventArgs e)
    {
        if (sender is not ClassesViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ClassCreateViewModel>();
        var window = new ClassCreateWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        window.ShowDialog();
    }

    private async void OnBookMember(object? sender, (int Id, string Name) args)
    {
        if (sender is not ClassesViewModel vm) return;
        var sp = ((System.Windows.Application.Current) as App)?.Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ClassBookViewModel>();
        var window = new ClassBookWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(args.Id, args.Name);
        window.ShowDialog();
    }
}
