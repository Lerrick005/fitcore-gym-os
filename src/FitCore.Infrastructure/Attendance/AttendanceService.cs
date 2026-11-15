using FitCore.Application.Abstractions;
using FitCore.Application.Attendance;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Attendance;

public sealed class AttendanceService : IAttendanceService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly ISettingsService _settings;

    public AttendanceService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, ISettingsService settings)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _audit = audit;
        _settings = settings;
    }

    public async Task<Result<CheckInResult>> CheckInAsync(CheckInRequest request, CancellationToken ct = default)
    {
        if (!_user.HasPermission("attendance.checkin"))
            return Result<CheckInResult>.Fail("permission.denied", "You do not have permission to check in members.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, ct);
        if (member is null) return Result<CheckInResult>.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is not null)
            return Result<CheckInResult>.Fail("member.archived", "Archived member cannot check in.");

        var rules = await _settings.GetAsync<RulesSettings>(ct);
        var now = _clock.UtcNow;

        // Find active or frozen membership
        var activeMembership = await _db.Memberships
            .AsQueryable()
            .Where(m => m.MemberId == member.Id)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(ct);

        var resultCode = "Denied";
        var message = "Unable to check in.";
        var isValid = false;

        if (activeMembership is not null)
        {
            var today = now.Date;

            if (activeMembership.Status == MembershipStatus.Frozen)
            {
                resultCode = "Frozen";
                message = "Membership is frozen.";
            }
            else if (activeMembership.EndDate.Date < today)
            {
                if (rules.ExpiredMembersCanCheckIn)
                {
                    resultCode = "Expired";
                    message = "Membership has expired (allowed by policy).";
                    isValid = true;
                }
                else
                {
                    resultCode = "Expired";
                    message = "Membership has expired.";
                }
            }
            else
            {
                var daysRemaining = (int)(activeMembership.EndDate.Date - today).TotalDays;
                if (daysRemaining <= 7)
                {
                    resultCode = "ExpiringSoon";
                    message = $"Membership expires in {daysRemaining} day(s).";
                    isValid = true;
                }
                else
                {
                    resultCode = "Valid";
                    message = "Membership is valid.";
                    isValid = true;
                }
            }
        }
        else
        {
            resultCode = "Expired";
            message = "No active membership.";
        }

        // Record the attempt
        var attendance = new AttendanceRecord
        {
            MemberId = member.Id,
            CheckInUtc = now,
            Method = CheckInMethod.ManualSearch,
            Result = ParseResultCode(resultCode),
            RecordedBy = _user.UserId ?? 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = _user.UserId
        };
        _db.AttendanceRecords.Add(attendance);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("attendance.checkin", "Attendance", attendance.Id.ToString(),
            null, $"member={member.MemberNumber} result={resultCode}", request.Notes, ct);

        return Result<CheckInResult>.Ok(new CheckInResult(isValid, resultCode, message, now));
    }

    public async Task<Result<IReadOnlyList<AttendanceListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<IReadOnlyList<AttendanceListItem>>.Fail("permission.denied", "You do not have permission to view attendance.");

        var list = await _db.AttendanceRecords
            .AsQueryable()
            .Where(a => a.MemberId == memberId)
            .OrderByDescending(a => a.CheckInUtc)
            .Select(a => new AttendanceListItem(
                a.Id,
                a.MemberId,
                a.CheckInUtc,
                a.CheckOutUtc,
                a.Method.ToString(),
                a.Result.ToString()))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AttendanceListItem>>.Ok(list);
    }

    private static AttendanceResult ParseResultCode(string code) => code switch
    {
        "Valid" => AttendanceResult.Valid,
        "ExpiringSoon" => AttendanceResult.ExpiringSoon,
        "Expired" => AttendanceResult.Expired,
        "Frozen" => AttendanceResult.Frozen,
        _ => AttendanceResult.Denied
    };

    public async Task<Result<IReadOnlyList<AttendanceListItem>>> ListRecentAsync(int take, CancellationToken ct = default)
    {
        if (!_user.HasPermission("attendance.checkin"))
            return Result<IReadOnlyList<AttendanceListItem>>.Fail("permission.denied", "You do not have permission to view attendance.");

        var items = await _db.AttendanceRecords
            .AsQueryable()
            .OrderByDescending(a => a.CheckInUtc)
            .Take(Math.Max(1, Math.Min(take, 1000)))
            .Select(a => new AttendanceListItem(
                a.Id, a.MemberId, a.CheckInUtc, a.CheckOutUtc,
                a.Method.ToString(), a.Result.ToString()))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AttendanceListItem>>.Ok(items);
    }

    public async Task<Result<int>> TodayCheckInCountAsync(CancellationToken ct = default)
    {
        var today = _clock.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var count = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.AttendanceRecords.AsQueryable()
                .Where(a => a.CheckInUtc >= today && a.CheckInUtc < tomorrow), ct);
        return Result<int>.Ok(count);
    }

    public async Task<Result<int>> CheckOutAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("attendance.checkin"))
            return Result<int>.Fail("permission.denied", "You do not have permission to check out members.");

        var now = _clock.UtcNow;
        var businessDayStart = now.Date;

        // Find the latest check-in today that hasn't been checked out
        var record = await _db.AttendanceRecords
            .AsQueryable()
            .Where(a => a.MemberId == memberId && a.CheckOutUtc == null && a.CheckInUtc >= businessDayStart)
            .OrderByDescending(a => a.CheckInUtc)
            .FirstOrDefaultAsync(ct);

        if (record is null)
            return Result<int>.Fail("attendance.not_checked_in", "Member is not currently checked in.");

        record.CheckOutUtc = now;
        record.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);

        var minutes = (int)(now - record.CheckInUtc).TotalMinutes;
        await _audit.LogAsync("attendance.checkout", "Attendance", record.Id.ToString(),
            null, $"durationMinutes={minutes}", null, ct);

        return Result<int>.Ok(minutes);
    }

    public async Task<Result<IReadOnlyList<AttendanceListItem>>> ListCurrentlyCheckedInAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission("attendance.checkin"))
            return Result<IReadOnlyList<AttendanceListItem>>.Fail("permission.denied", "You do not have permission to view attendance.");

        var today = _clock.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var list = await _db.AttendanceRecords
            .AsQueryable()
            .Where(a => a.CheckOutUtc == null && a.CheckInUtc >= today && a.CheckInUtc < tomorrow)
            .OrderBy(a => a.CheckInUtc)
            .Select(a => new AttendanceListItem(
                a.Id, a.MemberId, a.CheckInUtc, a.CheckOutUtc,
                a.Method.ToString(), a.Result.ToString()))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AttendanceListItem>>.Ok(list);
    }
}


