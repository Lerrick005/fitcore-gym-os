using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Training;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class TrainersViewModel : PageViewModelBase
{
    private readonly ITrainerService _trainers;

    public ObservableCollection<TrainerListItem> Rows { get; } = new();
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showInactive;

    public override string Title => "Trainers";

    public event EventHandler<int?>? EditRequested;
    public event EventHandler<int>? ToggleRequested;

    public TrainersViewModel(ITrainerService trainers) => _trainers = trainers;

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var r = await _trainers.ListAsync(ShowInactive, ct);
            Rows.Clear();
            if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
            foreach (var t in r.Value!) Rows.Add(t);
        }
        finally { IsBusy = false; }
    }

    partial void OnShowInactiveChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private void AddNew() => EditRequested?.Invoke(this, null);

    [RelayCommand]
    private void EditTrainer(TrainerListItem? row) => EditRequested?.Invoke(this, row?.Id);

    [RelayCommand]
    private void ToggleActive(TrainerListItem? row)
    {
        if (row is null) return;
        ToggleRequested?.Invoke(this, row.Id);
    }
}
