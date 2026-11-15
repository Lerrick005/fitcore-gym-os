using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Classes;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class ClassAttendeesViewModel : PageViewModelBase
{
    private readonly IClassesService _classes;
    private int _programmeId;

    [ObservableProperty] private string _className = string.Empty;
    [ObservableProperty] private string _classSchedule = string.Empty;
    [ObservableProperty] private int _capacity = 20;
    [ObservableProperty] private int _bookedCount;
    [ObservableProperty] private int _attendedCount;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public ObservableCollection<ClassAttendeeRow> Attendees { get; } = new();

    public override string Title => ClassName;

    public event EventHandler? AddAttendeeRequested;

    public ClassAttendeesViewModel(IClassesService classes) => _classes = classes;

    public void SetProgramme(int programmeId, string className, string classSchedule, int capacity)
    {
        _programmeId = programmeId;
        ClassName = className;
        ClassSchedule = classSchedule;
        Capacity = capacity;
        StatusMessage = null;
    }

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (_programmeId == 0) return;

        IsBusy = true;
        try
        {
            var r = await _classes.ListAttendeesAsync(_programmeId, ct);
            Attendees.Clear();
            if (r.IsSuccess)
            {
                foreach (var a in r.Value!) Attendees.Add(a);
                BookedCount = Attendees.Count;
                AttendedCount = 0;
                foreach (var a in Attendees) if (a.Status == "Attended") AttendedCount++;
            }
            else
            {
                StatusMessage = r.ErrorMessage;
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CheckIn(ClassAttendeeRow? row)
    {
        if (row is null) return;
        await _classes.UpdateBookingStatusAsync(row.BookingId, "Attended");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task MarkNoShow(ClassAttendeeRow? row)
    {
        if (row is null) return;
        await _classes.UpdateBookingStatusAsync(row.BookingId, "NoShow");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CancelBooking(ClassAttendeeRow? row)
    {
        if (row is null) return;
        await _classes.UpdateBookingStatusAsync(row.BookingId, "Cancelled");
        await RefreshAsync();
    }

    [RelayCommand]
    private void AddAttendee() => AddAttendeeRequested?.Invoke(this, EventArgs.Empty);
}
