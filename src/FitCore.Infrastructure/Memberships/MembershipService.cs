using System.Text.Json;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Memberships;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Memberships;

public sealed class MembershipService : IMembershipService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly ISettingsService _settings;

    public MembershipService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, ISettingsService settings)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _audit = audit;
        _settings = settings;
    }

    public async Task<Result<Membership>> SellAsync(SellMembershipRequest request, CancellationToken ct = default)
    {
        if (!_user.HasPermission("memberships.sell"))
            return Result<Membership>.Fail("permission.denied", "You do not have permission to sell memberships.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, ct);
        if (member is null) return Result<Membership>.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is not null) return Result<Membership>.Fail("member.archived", "Cannot sell to an archived member.");

        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);
        if (plan is null) return Result<Membership>.Fail("plan.not_found", "Plan not found.");
        if (!plan.IsActive) return Result<Membership>.Fail("plan.inactive", "Plan is not currently active.");

        // Block if member has an active (or frozen) membership
        var activeExists = await _db.Memberships.AnyAsync(m =>
            m.MemberId == request.MemberId &&
            (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Frozen), ct);
        if (activeExists)
            return Result<Membership>.Fail("membership.active_exists",
                "This member already has an active or frozen membership. Cancel or wait until it expires.");

        // Compute start + end
        var now = _clock.UtcNow;
        var startDate = (request.StartDate ?? now).Date;
        var endDate = AddDuration(startDate, plan.DurationValue, plan.DurationUnit);

        // Snapshot
        var termsSnapshot = JsonSerializer.Serialize(new
        {
            planId = plan.Id,
            name = plan.Name,
            freezeAllowanceDays = plan.FreezeAllowanceDays,
            visitLimit = plan.VisitLimit,
            joiningFeeMinor = plan.JoiningFeeMinor
        });

        var now2 = _clock.UtcNow;
        var membership = new Membership
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            PlanNameSnapshot = plan.Name,
            PriceMinorSnapshot = plan.PriceMinor,
            CurrencyCodeSnapshot = plan.CurrencyCode,
            DurationSnapshot = plan.DurationValue,
            TermsSnapshotJson = termsSnapshot,
            StartDate = startDate,
            EndDate = endDate,
            Status = MembershipStatus.Active,
            DiscountId = request.DiscountId,
            CreatedAtUtc = now2,
            UpdatedAtUtc = now2,
            CreatedBy = _user.UserId
        };

        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("membership.sell", "Membership", membership.Id.ToString(),
            null, $"plan={plan.Name} start={startDate:yyyy-MM-dd} end={endDate:yyyy-MM-dd}", null, ct);

        return Result<Membership>.Ok(membership);
    }

    public async Task<Result<Membership>> FreezeAsync(int membershipId, DateTime frozenFrom, DateTime frozenTo, string? reason, CancellationToken ct = default)
    {
        if (!_user.HasPermission("memberships.freeze"))
            return Result<Membership>.Fail("permission.denied", "You do not have permission to freeze memberships.");

        if (frozenTo <= frozenFrom)
            return Result<Membership>.Fail("validation.frozen_range", "Frozen-to date must be after frozen-from.");

        var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct);
        if (membership is null) return Result<Membership>.Fail("membership.not_found", "Membership not found.");
        if (membership.Status != MembershipStatus.Active)
            return Result<Membership>.Fail("membership.not_active", "Only active memberships can be frozen.");

        var rules = await _settings.GetAsync<RulesSettings>(ct);
        var freezeDays = (int)(frozenTo - frozenFrom).TotalDays;
        if (rules.MaxFreezeDays > 0 && freezeDays > rules.MaxFreezeDays)
            return Result<Membership>.Fail("membership.freeze_too_long",
                $"Freeze cannot exceed {rules.MaxFreezeDays} days.");

        var now = _clock.UtcNow;
        membership.Status = MembershipStatus.Frozen;
        membership.FrozenFrom = frozenFrom;
        membership.FrozenTo = frozenTo;
        membership.UpdatedAtUtc = now;

        // Extend end date by the freeze duration
        membership.EndDate = membership.EndDate.AddDays(freezeDays);

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("membership.freeze", "Membership", membership.Id.ToString(),
            null, $"frozen {frozenFrom:yyyy-MM-dd} → {frozenTo:yyyy-MM-dd}", reason, ct);

        return Result<Membership>.Ok(membership);
    }

    public async Task<Result<Membership>> UnfreezeAsync(int membershipId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("memberships.freeze"))
            return Result<Membership>.Fail("permission.denied", "You do not have permission to unfreeze memberships.");

        var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct);
        if (membership is null) return Result<Membership>.Fail("membership.not_found", "Membership not found.");
        if (membership.Status != MembershipStatus.Frozen)
            return Result<Membership>.Fail("membership.not_frozen", "This membership is not frozen.");

        var now = _clock.UtcNow;
        membership.Status = MembershipStatus.Active;
        membership.FrozenFrom = null;
        membership.FrozenTo = null;
        membership.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("membership.unfreeze", "Membership", membership.Id.ToString(), null, null, null, ct);

        return Result<Membership>.Ok(membership);
    }

    public async Task<Result> CancelAsync(int membershipId, string? reason, CancellationToken ct = default)
    {
        if (!_user.HasPermission("memberships.sell"))
            return Result.Fail("permission.denied", "You do not have permission to cancel memberships.");

        var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct);
        if (membership is null) return Result.Fail("membership.not_found", "Membership not found.");
        if (membership.Status == MembershipStatus.Cancelled) return Result.Ok();

        var now = _clock.UtcNow;
        membership.Status = MembershipStatus.Cancelled;
        membership.CancelledAtUtc = now;
        membership.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("membership.cancel", "Membership", membership.Id.ToString(), null, null, reason, ct);
        return Result.Ok();
    }

    public async Task<Result<Membership>> GetActiveForMemberAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<Membership>.Fail("permission.denied", "You do not have permission to view members.");

        var now = _clock.UtcNow.Date;
        var memberships = await _db.Memberships
            .AsQueryable()
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync(ct);

        foreach (var m in memberships)
        {
            // Auto-expire if past EndDate and still marked active
            if (m.Status == MembershipStatus.Active && m.EndDate.Date < now)
            {
                m.Status = MembershipStatus.Expired;
                m.UpdatedAtUtc = _clock.UtcNow;
                await _db.SaveChangesAsync(ct);
                continue;
            }

            if (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Frozen)
                return Result<Membership>.Ok(m);
        }

        return Result<Membership>.Fail("membership.none_active", "Member has no active membership.");
    }

    public async Task<Result<IReadOnlyList<MembershipListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<IReadOnlyList<MembershipListItem>>.Fail("permission.denied", "You do not have permission to view members.");

        var results = await _db.Memberships
            .AsQueryable()
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.StartDate)
            .Select(m => new MembershipListItem(
                m.Id,
                m.MemberId,
                m.PlanNameSnapshot,
                m.PriceMinorSnapshot,
                m.CurrencyCodeSnapshot,
                m.StartDate,
                m.EndDate,
                m.Status.ToString(),
                m.FrozenFrom,
                m.FrozenTo))
            .ToListAsync(ct);

        return Result<IReadOnlyList<MembershipListItem>>.Ok(results);
    }

    private static DateTime AddDuration(DateTime start, int value, DurationUnit unit) => unit switch
    {
        DurationUnit.Days => start.AddDays(value),
        DurationUnit.Weeks => start.AddDays(value * 7),
        DurationUnit.Months => start.AddMonths(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}
