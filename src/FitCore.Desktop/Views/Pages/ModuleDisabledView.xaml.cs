using System.Windows;
using System.Windows.Controls;

namespace FitCore.Desktop.Views.Pages;

public partial class ModuleDisabledView : UserControl
{
    public ModuleDisabledView() => InitializeComponent();

    private void GoToSettings_Click(object sender, RoutedEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        var services = app.Services;
        if (services is null) return;

        var nav = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<FitCore.Application.Navigation.INavigationService>(services);
        _ = nav.NavigateAsync("settings");
    }
}
