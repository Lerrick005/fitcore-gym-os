using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Modules;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.Views.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class ClassAttendeesView : UserControl
{
    public ClassAttendeesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClassAttendeesViewModel vm)
        {
            vm.AddAttendeeRequested += OnAddAttendee;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClassAttendeesViewModel vm)
        {
            vm.AddAttendeeRequested -= OnAddAttendee;
        }
    }

    private async void OnAddAttendee(object? sender, EventArgs e)
    {
        if (sender is not ClassAttendeesViewModel vm) return;

        // We need the programme ID to book a member. 
        // Since we don't have it directly on the VM, we can extract it from the first attendee, 
        // or better, we should have stored it. Let's ask the ViewModel via reflection or a public property.
        // For safety, let's assume we can grab it from the current page scope. 
        // Actually, the cleanest way is to expose the ProgrammeId on the VM.
        
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        // The VM doesn't currently expose _programmeId, so let's use reflection to get it
        // (Or we can just look at any existing booking to find the programme ID)
        var field = typeof(ClassAttendeesViewModel).GetField("_programmeId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;
        int programmeId = (int)(field.GetValue(vm) ?? 0);
        if (programmeId == 0) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<ClassBookViewModel>();
        var window = new ClassBookWindow { DataContext = editVm, Owner = Window.GetWindow(this) };

        editVm.CloseRequested += async (_, ok) =>
        {
            window.Close();
            if (ok) await vm.RefreshAsync();
        };

        await editVm.InitializeAsync(programmeId, vm.ClassName);
        window.ShowDialog();
    }
}
