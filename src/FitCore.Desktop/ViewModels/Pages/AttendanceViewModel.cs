using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Attendance;
using FitCore.Application.Formatting;
using FitCore.Application.Members;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed class AttendanceRow
{
    public int Id { get; init; }
    public int MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string CheckInDisplay { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
}

public sealed partial class AttendanceViewModel : PageViewModelBase
{

    public ObservableCollection<AttendanceRow> CurrentlyIn { get; } = new();
    private readonly IAttendanceService _attendance;
    private readonly IMemberService _members;
    private readonly IDateTimeFormatter _dates;
    private readonly FitCoreDbContext _db;


    [ObservableProperty] private ObservableCollection<AttendanceRow> _results = new();
    [ObservableProperty] private string _memberSearchText = string.Empty;
    [ObservableProperty] private int _todayCount;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasResults;

    public override string Title => "Attendance";

    public event EventHandler<int>? CheckInRequested;

    public AttendanceViewModel(
        IAttendanceService attendance,
        IMemberService members,
        IDateTimeFormatter dates,
        FitCoreDbContext db)
    {
        _attendance = attendance;
        _members = members;
        _dates = dates;
        _db = db;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var recent = await _attendance.ListRecentAsync(100, ct);
            if (!recent.IsSuccess)
            {
                StatusMessage = recent.ErrorMessage;
                Results.Clear();
                HasResults = false;
                return;
            }

            var todayCount = await _attendance.TodayCheckInCountAsync(ct);
            await RefreshCurrentlyInAsync();
            if (todayCount.IsSuccess) TodayCount = todayCount.Value;

            var memberIds = recent.Value!.Select(a => a.MemberId).Distinct().ToList();
            var members = await _db.Members
                .AsQueryable()
                .Where(m => memberIds.Contains(m.Id))
                .Select(m => new { m.Id, m.FullName, m.MemberNumber })
                .ToListAsync(ct);
            var map = members.ToDictionary(m => m.Id);

            Results.Clear();
            foreach (var a in recent.Value!)
            {
                var date = await _dates.FormatDateTimeAsync(a.CheckInUtc, ct);
                map.TryGetValue(a.MemberId, out var m);
                Results.Add(new AttendanceRow
                {
                    Id = a.Id,
                    MemberId = a.MemberId,
                    MemberName = m?.FullName ?? "(unknown)",
                    MemberNumber = m?.MemberNumber ?? "",
                    CheckInDisplay = date,
                    Method = a.Method,
                    Result = a.Result
                });
            }
            HasResults = Results.Count > 0;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CheckInBySearchAsync()
    {
        if (string.IsNullOrWhiteSpace(MemberSearchText))
        {
            StatusMessage = "Enter a name or member number to check in.";
            return;
        }

        var search = await _members.SearchAsync(new MemberSearchQuery { Text = MemberSearchText.Trim(), Take = 10 });
        if (!search.IsSuccess || search.Value!.Count == 0)
        {
            StatusMessage = "No matching member found.";
            return;
        }
        if (search.Value.Count > 1)
        {
            StatusMessage = $"{search.Value.Count} members match. Please be more specific.";
            return;
        }

        var member = search.Value[0];
        CheckInRequested?.Invoke(this, member.Id);
        MemberSearchText = string.Empty;
    }

    [RelayCommand]
    private async Task RefreshCommandAsync() => await RefreshAsync();

    [RelayCommand]
    private void ManualCheckIn(AttendanceRow? row)
    {
        if (row is null) return;
        CheckInRequested?.Invoke(this, row.MemberId);
    }

    [RelayCommand]
    private async Task CheckOutAsync(AttendanceRow? row)
    {
        if (row is null) return;
        var r = await _attendance.CheckOutAsync(row.MemberId);
        if (r.IsSuccess)
            StatusMessage = $"{row.MemberName} checked out — {r.Value} min.";
        else
            StatusMessage = r.ErrorMessage;

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshCurrentlyInAsync()
    {
        var r = await _attendance.ListCurrentlyCheckedInAsync();
        if (!r.IsSuccess) return;
        CurrentlyIn.Clear();
        foreach (var a in r.Value!)
        {
            var date = await _dates.FormatDateTimeAsync(a.CheckInUtc);
            var map = await _db.Members.AsQueryable().Where(m => m.Id == a.MemberId).Select(m => new { m.FullName, m.MemberNumber }).FirstOrDefaultAsync();
            CurrentlyIn.Add(new AttendanceRow
            {
                Id = a.Id,
                MemberId = a.MemberId,
                MemberName = map?.FullName ?? "(unknown)",
                MemberNumber = map?.MemberNumber ?? "",
                CheckInDisplay = date,
                Method = a.Method,
                Result = a.Result
            });
        }
    }
}

