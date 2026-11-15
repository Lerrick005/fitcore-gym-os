using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages.Settings;

namespace FitCore.Desktop.Views.Pages.Settings;

public partial class UserEditWindow : Window
{
    public UserEditWindow() => InitializeComponent();
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditViewModel vm && sender is PasswordBox pb) vm.Password = pb.Password;
    }
    private void ConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditViewModel vm && sender is PasswordBox pb) vm.ConfirmPassword = pb.Password;
    }
}
