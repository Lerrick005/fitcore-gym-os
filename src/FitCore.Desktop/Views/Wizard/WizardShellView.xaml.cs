using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace FitCore.Desktop.Views.Wizard;

public partial class WizardShellView : UserControl
{
    public WizardShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When the current step changes, scroll to top
        if (e.PropertyName == "CurrentStepViewModel")
        {
            ContentScroll?.ScrollToTop();
        }
    }
}
