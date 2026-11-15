using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Classes;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class ClassCreateViewModel : ObservableObject
{
    private readonly IClassesService _classes;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _selectedDay = "Monday";
    [ObservableProperty] private string _startTimeText = "18:00";
    [ObservableProperty] private int _durationMinutes = 60;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] DaysOfWeek { get; } = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    public event EventHandler<bool>? CloseRequested;

    public ClassCreateViewModel(IClassesService classes) => _classes = classes;

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Class name is required."; return; }

        // Parse the time (HH:mm format)
        if (!TimeSpan.TryParse(StartTimeText, out var timeSpan))
        {
            ErrorMessage = "Invalid time format. Please use HH:mm (e.g. 18:00).";
            return;
        }

        // Parse the day of week
        if (!Enum.TryParse<DayOfWeek>(SelectedDay, out var targetDay))
        {
            ErrorMessage = "Invalid day of week.";
            return;
        }

        // Compute the next occurrence of this day from today
        var today = DateTime.Today;
        var daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0 && timeSpan < DateTime.Now.TimeOfDay)
        {
            // If today and time has already passed, schedule for next week
            daysUntilTarget = 7;
        }
        var startDateTime = today.AddDays(daysUntilTarget).Add(timeSpan);

        IsBusy = true;
        try
        {
            var input = new ClassInput(
                Name.Trim(),
                string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                startDateTime,
                DurationMinutes);

            var r = await _classes.CreateClassAsync(input);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
