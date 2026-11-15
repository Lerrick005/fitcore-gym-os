using System.ComponentModel;
using System.Windows;

namespace FitCore.Desktop.Views.Wizard;

public partial class WizardWindow : Window
{
    public WizardWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Fit within screen work area (excludes taskbar)
        var work = SystemParameters.WorkArea;
        if (Height > work.Height - 40) Height = work.Height - 40;
        if (Width > work.Width - 40) Width = work.Width - 40;
        // Re-center
        Left = work.Left + (work.Width - Width) / 2;
        Top = work.Top + (work.Height - Height) / 2;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow != this) return;
        var result = MessageBox.Show(
            "Setup is not finished. If you close now, you can resume later on the next launch.\n\nClose anyway?",
            "FitCore Setup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) e.Cancel = true;
    }
}
