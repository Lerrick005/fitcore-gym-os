using System.Windows;
using System.Windows.Controls;
using FitCore.Application.Recovery;
using FitCore.Desktop.ViewModels.Login;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Login;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, __) => UsernameBox.Focus();
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        var services = app.Services;
        if (services is null)
        {
            MessageBox.Show("Service container not available.", "FitCore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var challenges = services.GetRequiredService<IChallengeService>();
        var vm = new ForgotPasswordViewModel(challenges);
        var window = new ForgotPasswordWindow { DataContext = vm, Owner = this };
        vm.RecoveryCompleted += (_, __) =>
        {
            MessageBox.Show("Your password has been reset. Sign in with the new password.",
                "FitCore", MessageBoxButton.OK, MessageBoxImage.Information);
            window.Close();
        };
        window.ShowDialog();
    }
}
