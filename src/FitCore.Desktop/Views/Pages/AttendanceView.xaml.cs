using System.Windows;
using System.Windows.Controls;
using FitCore.Application.Attendance;
using FitCore.Desktop.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages;

public partial class AttendanceView : UserControl
{
    public AttendanceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttendanceViewModel vm)
            vm.CheckInRequested += OnCheckIn;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AttendanceViewModel vm)
            vm.CheckInRequested -= OnCheckIn;
    }

    private async void OnCheckIn(object? sender, int memberId)
    {
        if (sender is not AttendanceViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = ServiceProviderServiceExtensions.CreateScope(sp);
        var attendance = ServiceProviderServiceExtensions.GetRequiredService<IAttendanceService>(scope.ServiceProvider);

        var result = await attendance.CheckInAsync(new CheckInRequest(memberId, null));
        if (!result.IsSuccess)
        {
            MessageBox.Show(result.ErrorMessage ?? "Check-in failed.", "Check-in",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var r = result.Value!;
        MessageBox.Show(r.Message, $"Check-in: {r.ResultCode}", MessageBoxButton.OK,
            r.IsValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
        await vm.RefreshAsync();
    }
}
