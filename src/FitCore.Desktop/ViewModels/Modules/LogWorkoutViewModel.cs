using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Progress;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class LogWorkoutViewModel : ObservableObject
{
    private readonly IProgressService _progress;
    private int _memberId;

    [ObservableProperty] private string _exerciseName = string.Empty;
    [ObservableProperty] private int _sets = 3;
    [ObservableProperty] private int _reps = 10;
    [ObservableProperty] private decimal _weightKg = 20m;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] CommonExercises { get; } = new[]
    {
        "Squat", "Bench Press", "Deadlift", "Overhead Press",
        "Barbell Row", "Pull Up", "Bicep Curl", "Tricep Extension",
        "Leg Press", "Lunge", "Plank"
    };

    public event EventHandler<bool>? CloseRequested;

    public LogWorkoutViewModel(IProgressService progress) => _progress = progress;

    public void Initialize(int memberId) { _memberId = memberId; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(ExerciseName)) { ErrorMessage = "Exercise name is required."; return; }
        if (Sets <= 0 || Reps <= 0) { ErrorMessage = "Sets and reps must be positive."; return; }

        IsBusy = true;
        try
        {
            var input = new WorkoutInput(_memberId, ExerciseName.Trim(), Sets, Reps, WeightKg,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());
            var r = await _progress.LogWorkoutAsync(input);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
