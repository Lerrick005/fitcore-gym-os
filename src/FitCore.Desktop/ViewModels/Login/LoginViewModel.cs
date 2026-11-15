using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Identity;

namespace FitCore.Desktop.ViewModels.Login;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _auth;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    /// <summary>Raised when login succeeds.</summary>
    public event EventHandler<AuthenticatedUser>? LoginSucceeded;

    public LoginViewModel(IAuthenticationService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy) return;

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Password is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Username.Trim(), Password);
            if (result.IsSuccess && result.Value is not null)
            {
                Password = string.Empty;
                LoginSucceeded?.Invoke(this, result.Value);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
