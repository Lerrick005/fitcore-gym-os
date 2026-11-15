using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Classes;
using FitCore.Application.Navigation;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class ClassesViewModel : PageViewModelBase
{
    private readonly IClassesService _classes;
    private readonly INavigationService _nav;

    public ObservableCollection<ClassRow> Classes { get; } = new();
    public ObservableCollection<ClassBookingRow> Bookings { get; } = new();
    public ObservableCollection<DayColumn> ClassesByDay { get; } = new();
    public ObservableCollection<ClassRow> Unscheduled { get; } = new();

    [ObservableProperty] private bool _showInactive;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public override string Title => "Classes & Programs";

    public event EventHandler? AddClassRequested;
    public event EventHandler<(int Id, string Name)>? BookMemberRequested;

    public ClassesViewModel(IClassesService classes, INavigationService nav)
    {
        _classes = classes;
        _nav = nav;
    }

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var r1 = await _classes.ListClassesAsync(ShowInactive, ct);
            Classes.Clear();
            if (r1.IsSuccess) foreach (var c in r1.Value!) Classes.Add(c);

            var r2 = await _classes.ListBookingsAsync(null, ct);
            Bookings.Clear();
            if (r2.IsSuccess) foreach (var b in r2.Value!) Bookings.Add(b);

            ClassesByDay.Clear();
            Unscheduled.Clear();

            if (r1.IsSuccess)
            {
                foreach (var c in r1.Value!.Where(x => !x.StartTime.HasValue))
                {
                    Unscheduled.Add(c);
                }
            }

            var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var orderedDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            for (int d = 0; d < 7; d++)
            {
                var targetDay = orderedDays[d];
                var col = new DayColumn(dayNames[d], new ObservableCollection<ClassRow>());
                if (r1.IsSuccess)
                {
                    foreach (var c in r1.Value!.Where(x => x.StartTime.HasValue && x.StartTime.Value.DayOfWeek == targetDay && x.StartTime.Value.Date >= DateTime.Today.AddDays(-6)).OrderBy(x => x.StartTime))
                    {
                        col.Classes.Add(c);
                    }
                }
                ClassesByDay.Add(col);
            }
        }
        finally { IsBusy = false; }
    }

    partial void OnShowInactiveChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private void AddClass() => AddClassRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void BookMember(ClassRow? row)
    {
        if (row is null) return;
        BookMemberRequested?.Invoke(this, (row.Id, row.Name));
    }

    [RelayCommand]
    private async Task ManageAttendees(ClassRow? row)
    {
        if (row is null) { System.Windows.MessageBox.Show("DEBUG: row is null"); return; }
        try
        {
            // Get the LIVE ShellViewModel from the actual window, not from DI
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow is null) { System.Windows.MessageBox.Show("DEBUG: MainWindow is null"); return; }
            var shellVm = mainWindow.DataContext as FitCore.Desktop.ViewModels.Shell.ShellViewModel;
            if (shellVm is null) { System.Windows.MessageBox.Show("DEBUG: shellVm is not ShellViewModel — actual: " + mainWindow.DataContext?.GetType().FullName); return; }

            // Create the Attendees VM from DI
            var sp = ((App)System.Windows.Application.Current).Services;
            if (sp is null) { System.Windows.MessageBox.Show("DEBUG: DI Services is null"); return; }
            var attendeesVm = sp.GetService(typeof(ClassAttendeesViewModel)) as ClassAttendeesViewModel;
            if (attendeesVm is null) { System.Windows.MessageBox.Show("DEBUG: ClassAttendeesViewModel NOT in DI"); return; }

            var schedule = row.StartTime.HasValue ? row.StartTime.Value.ToString("dddd HH:mm") : "Unscheduled";
            attendeesVm.SetProgramme(row.Id, row.Name, schedule, 20);
            await attendeesVm.LoadAsync();

            shellVm.CurrentPage = attendeesVm;
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show("DEBUG Exception: " + ex.GetType().Name + " — " + ex.Message);
        }
    }
}

public sealed class DayColumn
{
    public string DayName { get; }
    public ObservableCollection<ClassRow> Classes { get; }
    public DayColumn(string dayName, ObservableCollection<ClassRow> classes)
    {
        DayName = dayName;
        Classes = classes;
    }
}
