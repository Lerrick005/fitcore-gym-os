using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class FollowUpCompleteViewModel : ObservableObject
{
    private int _followUpId;

    [ObservableProperty] private string _title = "Complete follow-up";
    [ObservableProperty] private string _memberName = string.Empty;
    [ObservableProperty] private string _outcome = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public void Initialize(int followUpId, string memberName)
    {
        _followUpId = followUpId;
        MemberName = memberName;
        Outcome = string.Empty;
        ErrorMessage = null;
        Title = "Complete follow-up";
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Outcome))
        {
            ErrorMessage = "Outcome is required.";
            return;
        }
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    public int FollowUpId => _followUpId;
    public string OutcomeValue => Outcome.Trim();
}
