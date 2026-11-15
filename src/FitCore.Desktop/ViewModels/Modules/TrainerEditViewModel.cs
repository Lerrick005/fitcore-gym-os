using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Training;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class TrainerEditViewModel : ObservableObject
{
    private readonly ITrainerService _trainers;
    private int? _trainerId;

    [ObservableProperty] private string _title = "Add trainer";
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _specialities = string.Empty;
    [ObservableProperty] private string _commissionRate = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public TrainerEditViewModel(ITrainerService trainers) => _trainers = trainers;

    public async Task InitializeAsync(int? trainerId, CancellationToken ct = default)
    {
        _trainerId = trainerId;
        Title = trainerId.HasValue ? "Edit trainer" : "Add trainer";
        if (!trainerId.HasValue) return;

        var r = await _trainers.GetAsync(trainerId.Value, ct);
        if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
        var t = r.Value!;
        FullName = t.FullName;
        Phone = t.Phone ?? string.Empty;
        Email = t.Email ?? string.Empty;
        Specialities = t.Specialities ?? string.Empty;
        CommissionRate = t.CommissionRate?.ToString("0.00") ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(FullName)) { ErrorMessage = "Full name is required."; return; }

        decimal? commission = null;
        if (!string.IsNullOrWhiteSpace(CommissionRate) && decimal.TryParse(CommissionRate, out var c))
            commission = c;

        IsBusy = true;
        try
        {
            var input = new TrainerInput(
                FullName.Trim(),
                string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                string.IsNullOrWhiteSpace(Specialities) ? null : Specialities.Trim(),
                commission);

            var r = _trainerId.HasValue
                ? await _trainers.UpdateAsync(_trainerId.Value, input)
                : await _trainers.CreateAsync(input);

            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
