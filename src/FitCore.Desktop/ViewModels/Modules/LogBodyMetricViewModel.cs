using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Progress;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class LogBodyMetricViewModel : ObservableObject
{
    private readonly IProgressService _progress;
    private int _memberId;

    [ObservableProperty] private decimal? _weightKg;
    [ObservableProperty] private decimal? _bodyFatPercent;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public LogBodyMetricViewModel(IProgressService progress) => _progress = progress;

    public void Initialize(int memberId) { _memberId = memberId; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (!WeightKg.HasValue && !BodyFatPercent.HasValue) 
        { ErrorMessage = "Enter at least one metric (weight or body fat)."; return; }

        IsBusy = true;
        try
        {
            var input = new BodyMetricInput(_memberId, WeightKg, BodyFatPercent, 
                string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());
            var r = await _progress.LogBodyMetricAsync(input);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
