using FitCore.Application.Abstractions;
using FitCore.Application.CashUp;
using FitCore.Application.Common;
using FitCore.Application.Formatting;
using FitCore.Application.Settings;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.CashUp;

public sealed class CashUpService : ICashUpService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly ISettingsService _settings;

    public CashUpService(FitCoreDbContext db, IClock clock, ISettingsService settings)
    {
        _db = db;
        _clock = clock;
        _settings = settings;
    }

    public async Task<Result<CashUpSummary>> GetDayAsync(DateTime date, CancellationToken ct = default)
    {
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var tz = TryFindZone(locale.TimeZoneId);

        // Business day = start-of-day in local tz → start-of-next-day in UTC
        var localDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var rows = await _db.Payments
            .AsQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.ReceivedAtUtc >= startUtc && p.ReceivedAtUtc < endUtc)
            .Select(p => new
            {
                p.ReceivedAtUtc,
                p.AmountMinor,
                p.Method,
                p.Reference,
                p.MemberId
            })
            .ToListAsync(ct);

        var memberIds = rows.Select(r => r.MemberId).Distinct().ToList();
        var members = await _db.Members
            .AsQueryable()
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, m.FullName, m.MemberNumber })
            .ToListAsync(ct);
        var map = members.ToDictionary(m => m.Id);

        var byMethod = rows
            .GroupBy(r => r.Method)
            .Select(g => new CashUpMethodLine(g.Key, g.Count(), g.Sum(x => x.AmountMinor)))
            .OrderByDescending(m => m.TotalMinor)
            .ToList();

        var receipts = rows
            .OrderBy(r => r.ReceivedAtUtc)
            .Select(r =>
            {
                map.TryGetValue(r.MemberId, out var m);
                return new CashUpReceiptLine(r.ReceivedAtUtc, m?.FullName ?? "(unknown)", m?.MemberNumber ?? "", r.Method, r.AmountMinor, r.Reference);
            })
            .ToList();

        var total = rows.Sum(r => r.AmountMinor);
        var currency = (await _db.Installations.AsQueryable().FirstOrDefaultAsync(ct)) is null ? "KES" : locale.CurrencyCode;

        return Result<CashUpSummary>.Ok(new CashUpSummary(date.Date, total, currency, byMethod, receipts));
    }

    private static TimeZoneInfo TryFindZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
