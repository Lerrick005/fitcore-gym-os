using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Recovery;

namespace FitCore.Desktop.ViewModels.Login;

/// <summary>
/// Three-step recovery flow: (1) request challenge, (2) paste recovery code,
/// (3) set new password. Matches PRS §11.
/// </summary>
public sealed partial class ForgotPasswordViewModel : ObservableObject
{
    private readonly IChallengeService _challenges;

    [ObservableProperty] private int _step = 1;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _challengeCode = string.Empty;
    [ObservableProperty] private string _recoveryCode = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _infoMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler? RecoveryCompleted;

    public ForgotPasswordViewModel(IChallengeService challenges)
    {
        _challenges = challenges;
    }

    [RelayCommand]
    private async Task IssueChallengeAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        IsBusy = true;
        try
        {
            var result = await _challenges.IssueChallengeAsync(Username);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
            ChallengeCode = result.Value!.ChallengeCode;
            Step = 2;
            InfoMessage = "Send the challenge code to your software vendor. They will return a recovery code.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task VerifyRecoveryAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        IsBusy = true;
        try
        {
            var result = await _challenges.VerifyRecoveryAsync(ChallengeCode, RecoveryCode);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
            Step = 3;
            InfoMessage = "Recovery code accepted. Choose a new password.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CompleteRecoveryAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _challenges.CompleteRecoveryAsync(ChallengeCode, NewPassword);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
            RecoveryCompleted?.Invoke(this, EventArgs.Empty);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyChallengeCode()
    {
        try { System.Windows.Clipboard.SetText(ChallengeCode); InfoMessage = "Challenge code copied to clipboard."; }
        catch { InfoMessage = "Could not copy. Please select and copy manually."; }
    }
}
