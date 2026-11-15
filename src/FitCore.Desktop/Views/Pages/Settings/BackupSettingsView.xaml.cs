using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages.Settings;
using FitCore.Application.Backup;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Pages.Settings;

public partial class BackupSettingsView : UserControl
{
    public BackupSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BackupSettingsViewModel vm)
            vm.RestoreRequested += OnRestoreRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BackupSettingsViewModel vm)
            vm.RestoreRequested -= OnRestoreRequested;
    }

    private void OnRestoreRequested(object? sender, string backupPath)
    {
        var confirm = MessageBox.Show(
            $"Restore from backup?\n\n{backupPath}\n\n" +
            "Your current data will be replaced with the backup contents. " +
            "A safety backup will be taken first.\n\n" +
            "The app will close and you must restart it to see the restored data.",
            "Confirm restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        var backup = sp.GetRequiredService<IBackupService>();
        var result = backup.RestoreAsync(backupPath).GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            MessageBox.Show("Restore complete. The application will now close. Please restart FitCore.",
                "Restore successful", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            MessageBox.Show($"Restore failed: {result.ErrorMessage}", "Restore failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
