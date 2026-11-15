using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Loyalty;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Loyalty;

public sealed class LoyaltyService : ILoyaltyService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

    public LoyaltyService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules; _scopeFactory = scopeFactory;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("loyalty", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<ReferralRow>>> ListReferralsAsync(CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ReferralRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<IReadOnlyList<ReferralRow>>.Fail("permission.denied", "No permission.");

        var rows = await (from r in _db.Referrals.AsQueryable()
                          join referrer in _db.Members.AsQueryable() on r.ReferrerMemberId equals referrer.Id
                          join referee in _db.Members.AsQueryable() on r.RefereeMemberId equals (int?)referee.Id into refereeJoin
                          from referee in refereeJoin.DefaultIfEmpty()
                          orderby r.ReferredAtUtc descending
                          select new ReferralRow(r.Id, r.ReferralCode ?? "", referrer.FullName, referee != null ? referee.FullName : null, r.ReferredAtUtc, r.RewardGranted))
                          .Take(500).ToListAsync(ct);
        return Result<IReadOnlyList<ReferralRow>>.Ok(rows);
    }

    public async Task<Result<ReferralRow>> CreateReferralAsync(ReferralInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<ReferralRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<ReferralRow>.Fail("permission.denied", "No permission.");

        var referrer = await _db.Members.AsQueryable().FirstOrDefaultAsync(x => x.Id == input.ReferrerMemberId, ct);
        if (referrer is null) return Result<ReferralRow>.Fail("member.not_found", "Referrer not found.");

        var refRow = new Referral
        {
            ReferrerMemberId = input.ReferrerMemberId,
            ReferralCode = input.ReferralCode,
            ReferredAtUtc = _clock.UtcNow,
            RewardGranted = false,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        };
        _db.Referrals.Add(refRow);
        await _db.SaveChangesAsync(ct);
        return Result<ReferralRow>.Ok(new ReferralRow(refRow.Id, refRow.ReferralCode ?? "", referrer.FullName, null, refRow.ReferredAtUtc, false));
    }

    public async Task<Result> GrantReferralRewardAsync(int referralId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("loyalty.manage")) return Result.Fail("permission.denied", "No permission.");

        var refRow = await _db.Referrals.AsQueryable().FirstOrDefaultAsync(r => r.Id == referralId, ct);
        if (refRow is null) return Result.Fail("referral.not_found", "Referral not found.");
        refRow.RewardGranted = true;
        refRow.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("referral.reward_granted", "Referral", referralId.ToString(), null, null, null, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<LoyaltyEntryRow>>> ListEntriesAsync(int? memberId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<LoyaltyEntryRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<IReadOnlyList<LoyaltyEntryRow>>.Fail("permission.denied", "No permission.");

        var q = _db.LoyaltyLedger.AsQueryable();
        if (memberId.HasValue) q = q.Where(e => e.MemberId == memberId.Value);

        var rows = await (from e in q
                          join mm in _db.Members.AsQueryable() on e.MemberId equals mm.Id
                          orderby e.OccurredAtUtc descending
                          select new LoyaltyEntryRow(e.Id, mm.FullName, e.Type.ToString(), e.Points, e.OccurredAtUtc, e.Reason))
                          .Take(500).ToListAsync(ct);
        return Result<IReadOnlyList<LoyaltyEntryRow>>.Ok(rows);
    }

    public async Task<Result<LoyaltyEntryRow>> AddEntryAsync(LoyaltyEntryInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<LoyaltyEntryRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<LoyaltyEntryRow>.Fail("permission.denied", "No permission.");

        var member = await _db.Members.AsQueryable().FirstOrDefaultAsync(x => x.Id == input.MemberId, ct);
        if (member is null) return Result<LoyaltyEntryRow>.Fail("member.not_found", "Member not found.");

        var type = Enum.TryParse<LoyaltyEntryType>(input.Type, true, out var t) ? t : LoyaltyEntryType.Adjustment;
        var entry = new LoyaltyLedgerEntry
        {
            MemberId = input.MemberId,
            Type = type,
            Points = input.Points,
            OccurredAtUtc = _clock.UtcNow,
            Reason = input.Reason,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        };
        _db.LoyaltyLedger.Add(entry);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("loyalty.entry", "LoyaltyLedger", entry.Id.ToString(), null, $"{type} {input.Points}", input.Reason, ct);
        return Result<LoyaltyEntryRow>.Ok(new LoyaltyEntryRow(entry.Id, member.FullName, type.ToString(), entry.Points, entry.OccurredAtUtc, entry.Reason));
    }

    public async Task<Result<int>> GetBalanceAsync(int memberId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<int>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<int>.Fail("permission.denied", "No permission.");

        var entries = await _db.LoyaltyLedger.AsQueryable()
            .Where(e => e.MemberId == memberId)
            .ToListAsync(ct);

        var balance = 0;
        foreach (var e in entries)
        {
            balance += e.Type switch
            {
                LoyaltyEntryType.Earn => e.Points,
                LoyaltyEntryType.Redeem => -e.Points,
                LoyaltyEntryType.Adjustment => e.Points,
                _ => 0
            };
        }

        return Result<int>.Ok(Math.Max(0, balance));
    }

    public async Task<Result<long>> GetBalanceValueMinorAsync(int memberId, CancellationToken ct = default)
    {
        var bal = await GetBalanceAsync(memberId, ct);
        if (!bal.IsSuccess) return Result<long>.Fail(bal.ErrorCode!, bal.ErrorMessage!);

        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var loyalty = await settings.GetAsync<LoyaltySettings>(ct);

        var units = bal.Value / loyalty.PointsPerRedemptionUnit;
        return Result<long>.Ok(units * loyalty.RedemptionUnitValueMinor);
    }
}



