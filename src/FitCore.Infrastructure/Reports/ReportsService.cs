using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Reports;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Reports;

public sealed class ReportsService : IReportsService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;

    public ReportsService(FitCoreDbContext db, IClock clock, ICurrentUser user)
    {
        _db = db;
        _clock = clock;
        _user = user;
    }

    public async Task<Result<ReportsData>> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission("reports.view"))
            return Result<ReportsData>.Fail("permission.denied", "You do not have permission to view reports.");

        var now = _clock.UtcNow;
        var thirtyDaysAgo = now.Date.AddDays(-30);
        var thirtyDaysForward = now.Date.AddDays(30);

        // Revenue by day (last 30 days)
        var payments = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed
                     && p.ReceivedAtUtc >= thirtyDaysAgo
                     && p.ReceivedAtUtc <= now)
            .Select(p => new { p.ReceivedAtUtc, p.AmountMinor, p.CurrencyCode, p.Method })
            .ToListAsync(ct);

        var revenueByDay = payments
            .GroupBy(p => p.ReceivedAtUtc.Date)
            .Select(g => new RevenueByDay(g.Key, g.Sum(x => x.AmountMinor),
                g.Select(x => x.CurrencyCode).FirstOrDefault() ?? "KES"))
            .OrderBy(r => r.Date)
            .ToList();

        var revenueTotal = payments.Sum(p => p.AmountMinor);

        var revenueByMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new RevenueByMethod(g.Key, g.Sum(x => x.AmountMinor), g.Count()))
            .OrderByDescending(r => r.AmountMinor)
            .ToList();

        // Expiring soon
        var expiring = await _db.Memberships
            .AsQueryable()
            .Where(m => m.Status == MembershipStatus.Active
                     && m.EndDate >= now.Date
                     && m.EndDate <= thirtyDaysForward)
            .Join(_db.Members.AsQueryable(),
                ms => ms.MemberId, mb => mb.Id,
                (ms, mb) => new { ms, mb })
            .OrderBy(x => x.ms.EndDate)
            .Select(x => new ExpiringMember(
                x.mb.Id, x.mb.MemberNumber, x.mb.FullName, x.mb.Phone,
                x.ms.EndDate, x.ms.PlanNameSnapshot))
            .ToListAsync(ct);

        // Attendance by day (last 30 days)
        var attendance = await _db.AttendanceRecords
            .AsQueryable()
            .Where(a => a.CheckInUtc >= thirtyDaysAgo && a.CheckInUtc <= now)
            .Select(a => a.CheckInUtc)
            .ToListAsync(ct);

        var attendanceByDay = attendance
            .GroupBy(d => d.Date)
            .Select(g => new AttendanceByDay(g.Key, g.Count()))
            .OrderBy(a => a.Date)
            .ToList();

        // New members last 30 days
        var newMembers = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable()
                .Where(m => m.CreatedAtUtc >= thirtyDaysAgo), ct);

        // Active members
        var activeMembers = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable()
                .Where(m => m.Status == MemberStatus.Active && m.ArchivedAtUtc == null), ct);

        // ===== Analytics: 12-month revenue =====
        var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var allPayments = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.ReceivedAtUtc >= twelveMonthsAgo)
            .Select(p => new { p.ReceivedAtUtc, p.AmountMinor, p.CurrencyCode })
            .ToListAsync(ct);

        var revenueByMonth = allPayments
            .GroupBy(p => new { p.ReceivedAtUtc.Year, p.ReceivedAtUtc.Month })
            .Select(g => new RevenueByMonth(g.Key.Year, g.Key.Month, g.Sum(x => x.AmountMinor),
                g.Select(x => x.CurrencyCode).FirstOrDefault() ?? "KES"))
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ToList();

        // ===== Analytics: Member growth last 30 days =====
        var membersCreated = await _db.Members
            .AsQueryable()
            .Where(m => m.CreatedAtUtc >= thirtyDaysAgo)
            .Select(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        var growthByDay = new List<MemberGrowthByDay>();
        var cumulative = activeMembers - membersCreated.Count; // approx start-of-period
        for (var d = thirtyDaysAgo.Date; d <= now.Date; d = d.AddDays(1))
        {
            var dayCount = membersCreated.Count(c => c.Date == d);
            cumulative += dayCount;
            growthByDay.Add(new MemberGrowthByDay(d, dayCount, cumulative));
        }

        // ===== Analytics: Member growth last 12 months =====
        var twelveMonthMembers = await _db.Members
            .AsQueryable()
            .Where(m => m.CreatedAtUtc >= twelveMonthsAgo)
            .Select(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        var growthByMonth = twelveMonthMembers
            .GroupBy(c => new { c.Year, c.Month })
            .Select(g => new MemberGrowthByMonth(g.Key.Year, g.Key.Month, g.Count()))
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToList();

        // ===== Analytics: YTD and monthly totals =====
        var yearStart = new DateTime(now.Year, 1, 1);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var revenueYtd = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.ReceivedAtUtc >= yearStart && p.ReceivedAtUtc < now)
            .Select(p => (long?)p.AmountMinor)
            .SumAsync(ct) ?? 0;

        var revenueThisMonth = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.ReceivedAtUtc >= monthStart && p.ReceivedAtUtc < now)
            .Select(p => (long?)p.AmountMinor)
            .SumAsync(ct) ?? 0;

        var revenueLastMonth = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.ReceivedAtUtc >= lastMonthStart && p.ReceivedAtUtc < monthStart)
            .Select(p => (long?)p.AmountMinor)
            .SumAsync(ct) ?? 0;

        var membersAtStartOfYear = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable().Where(m => m.CreatedAtUtc < yearStart && m.ArchivedAtUtc == null), ct);
        var membersAtStartOfMonth = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable().Where(m => m.CreatedAtUtc < monthStart && m.ArchivedAtUtc == null), ct);

        return Result<ReportsData>.Ok(new ReportsData(
            revenueByDay,
            revenueByMethod,
            expiring,
            attendanceByDay,
            revenueTotal,
            newMembers,
            activeMembers,
            revenueByMonth,
            growthByDay,
            growthByMonth,
            revenueYtd,
            membersAtStartOfYear,
            membersAtStartOfMonth,
            revenueThisMonth,
            revenueLastMonth,
            revenueByDay.FirstOrDefault()?.CurrencyCode ?? "KES"));
    }
}

