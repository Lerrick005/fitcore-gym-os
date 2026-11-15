using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Progress;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class FitnessViewModel : PageViewModelBase
{
    private readonly IProgressService _progress;
    private int _selectedMemberId;

    public ObservableCollection<MemberProgressSummary> Members { get; } = new();
    public ObservableCollection<WorkoutRow> Workouts { get; } = new();
    public ObservableCollection<BodyMetricRow> BodyMetrics { get; } = new();

    [ObservableProperty] private MemberProgressSummary? _selectedMember;
    [ObservableProperty] private MemberProgressSummary? _selectedSummary;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public override string Title => "Fitness Tracker";

    public event EventHandler? LogWorkoutRequested;
    public event EventHandler? LogBodyMetricRequested;

    public FitnessViewModel(IProgressService progress) => _progress = progress;

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshMembersAsync(ct);

    public async Task RefreshMembersAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var r = await _progress.ListMembersWithProgressAsync(ct);
            Members.Clear();
            if (r.IsSuccess)
            {
                foreach (var m in r.Value!) Members.Add(m);
                
                // Auto-select the first member if none is selected
                if (Members.Count > 0 && SelectedMember == null)
                {
                    SelectedMember = Members[0];
                }
            }
            else StatusMessage = r.ErrorMessage;
        }
        finally { IsBusy = false; }
    }

    partial void OnSelectedMemberChanged(MemberProgressSummary? value)
    {
        if (value is null) return;
        _selectedMemberId = value.MemberId;
        _ = LoadMemberDetailAsync(value.MemberId);
    }

    private async Task LoadMemberDetailAsync(int memberId)
    {
        IsBusy = true;
        try
        {
            var workouts = await _progress.ListWorkoutsAsync(memberId, 50);
            Workouts.Clear();
            if (workouts.IsSuccess) foreach (var w in workouts.Value!) Workouts.Add(w);

            var metrics = await _progress.ListBodyMetricsAsync(memberId, 20);
            BodyMetrics.Clear();
            if (metrics.IsSuccess) foreach (var b in metrics.Value!) BodyMetrics.Add(b);

            var summary = await _progress.GetSummaryAsync(memberId);
            if (summary.IsSuccess) SelectedSummary = summary.Value;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Refresh() => await RefreshMembersAsync();

    [RelayCommand]
    private void LogWorkout() => LogWorkoutRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void LogBodyMetric() => LogBodyMetricRequested?.Invoke(this, EventArgs.Empty);

    public int SelectedMemberId => _selectedMemberId;
}
