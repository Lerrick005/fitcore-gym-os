using FitCore.Application.Abstractions;
using FitCore.Application.Dashboard;
using FitCore.Application.Settings;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly FitCoreDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IClock _clock;

    public DashboardService(FitCoreDbContext db, ISettingsService settings, IClock clock)
    {
        _db = db;
        _settings = settings;
        _clock = clock;
    }

    public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var thresholds = await _settings.GetAsync<ThresholdsSettings>(ct);

        var expiringSoonCutoff = today.AddDays(thresholds.DaysBeforeExpiryForReminder);

        // Active members: any member whose status is Active
        var activeMembers = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable().Where(m => m.Status == MemberStatus.Active && m.ArchivedAtUtc == null), ct);

        // Total members (non-archived)
        var totalMembers = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Members.AsQueryable().Where(m => m.ArchivedAtUtc == null), ct);

        // Expiring soon: active memberships ending between now and expiringSoonCutoff
        var expiringSoon = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Memberships.AsQueryable()
                .Where(m => m.Status == MembershipStatus.Active
                            && m.EndDate >= today
                            && m.EndDate <= expiringSoonCutoff), ct);

        // Expired: memberships whose EndDate has passed and still marked Active
        var expired = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Memberships.AsQueryable()
                .Where(m => m.Status == MembershipStatus.Active
                            && m.EndDate < today), ct);

        // Today's check-ins
        var todayCheckIns = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.AttendanceRecords.AsQueryable()
                .Where(a => a.CheckInUtc >= today && a.CheckInUtc < tomorrow), ct);

        // Today's revenue (completed payments)
        var todayRevenueMinor = await EntityFrameworkQueryableExtensions
            .SumAsync(_db.Payments.AsQueryable()
                .Where(p => p.ReceivedAtUtc >= today
                            && p.ReceivedAtUtc < tomorrow
                            && p.Status == PaymentStatus.Completed),
                p => (long?)p.AmountMinor, ct) ?? 0L;

        // Outstanding balances: members with unpaid balances (approximated as active memberships minus completed payments)
        var outstanding = await EntityFrameworkQueryableExtensions.CountAsync(
            _db.Memberships.AsQueryable()
                .Where(m => m.Status == MembershipStatus.Active
                            && m.EndDate >= today
                            && !_db.Payments.AsQueryable().Any(p =>
                                p.MembershipId == m.Id
                                && p.Status == PaymentStatus.Completed
                                && p.AmountMinor >= m.PriceMinorSnapshot)), ct);

        return new DashboardStats(
            ActiveMembers: activeMembers,
            ExpiringSoonMembers: expiringSoon,
            ExpiredMembers: expired,
            TodayCheckIns: todayCheckIns,
            TodayRevenueMinor: todayRevenueMinor,
            CurrencyCode: locale.CurrencyCode,
            OutstandingBalancesCount: outstanding,
            TotalMembers: totalMembers);
    }
}
