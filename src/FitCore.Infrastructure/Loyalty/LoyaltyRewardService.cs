using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Loyalty;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Loyalty;

public sealed class LoyaltyRewardService : ILoyaltyRewardService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;
    private readonly ILoyaltyService _loyalty;

    public LoyaltyRewardService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules, ILoyaltyService loyalty)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules; _loyalty = loyalty;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("loyalty", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<LoyaltyRewardRow>>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<LoyaltyRewardRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<IReadOnlyList<LoyaltyRewardRow>>.Fail("permission.denied", "No permission.");

        var q = _db.LoyaltyRewards.AsQueryable();
        if (!includeInactive) q = q.Where(r => r.IsActive);

        var rows = await q.OrderBy(r => r.PointsRequired)
            .Select(r => new LoyaltyRewardRow(r.Id, r.Name, r.Description, r.PointsRequired, r.RewardType, r.ValueMinor, r.ExpiresAtUtc, r.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyList<LoyaltyRewardRow>>.Ok(rows);
    }

    public async Task<Result<LoyaltyRewardRow>> CreateAsync(LoyaltyRewardInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<LoyaltyRewardRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<LoyaltyRewardRow>.Fail("permission.denied", "No permission.");
        if (string.IsNullOrWhiteSpace(input.Name)) return Result<LoyaltyRewardRow>.Fail("validation.name", "Name required.");
        if (input.PointsRequired <= 0) return Result<LoyaltyRewardRow>.Fail("validation.points", "Points must be positive.");

        var r = new LoyaltyReward
        {
            Name = input.Name.Trim(),
            Description = input.Description,
            PointsRequired = input.PointsRequired,
            RewardType = input.RewardType,
            ValueMinor = input.ValueMinor,
            ExpiresAtUtc = input.ExpiresAtUtc,
            IsActive = input.IsActive,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow,
            CreatedBy = _user.UserId
        };
        _db.LoyaltyRewards.Add(r);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("loyalty.reward.create", "LoyaltyReward", r.Id.ToString(), null, r.Name, null, ct);
        return Result<LoyaltyRewardRow>.Ok(new LoyaltyRewardRow(r.Id, r.Name, r.Description, r.PointsRequired, r.RewardType, r.ValueMinor, r.ExpiresAtUtc, r.IsActive));
    }

    public async Task<Result<LoyaltyRewardRow>> UpdateAsync(int id, LoyaltyRewardInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<LoyaltyRewardRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("loyalty.manage")) return Result<LoyaltyRewardRow>.Fail("permission.denied", "No permission.");

        var r = await _db.LoyaltyRewards.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Result<LoyaltyRewardRow>.Fail("reward.not_found", "Reward not found.");

        r.Name = input.Name.Trim();
        r.Description = input.Description;
        r.PointsRequired = input.PointsRequired;
        r.RewardType = input.RewardType;
        r.ValueMinor = input.ValueMinor;
        r.ExpiresAtUtc = input.ExpiresAtUtc;
        r.IsActive = input.IsActive;
        r.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("loyalty.reward.update", "LoyaltyReward", r.Id.ToString(), null, r.Name, null, ct);
        return Result<LoyaltyRewardRow>.Ok(new LoyaltyRewardRow(r.Id, r.Name, r.Description, r.PointsRequired, r.RewardType, r.ValueMinor, r.ExpiresAtUtc, r.IsActive));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("loyalty.manage")) return Result.Fail("permission.denied", "No permission.");

        var r = await _db.LoyaltyRewards.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Result.Fail("reward.not_found", "Reward not found.");
        r.IsActive = false;
        r.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("loyalty.reward.deactivate", "LoyaltyReward", r.Id.ToString(), null, r.Name, null, ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<RedeemableReward>>> ListRedeemableForMemberAsync(int memberId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<RedeemableReward>>.Fail(m.ErrorCode!, m.ErrorMessage!);

        var bal = await _loyalty.GetBalanceAsync(memberId, ct);
        if (!bal.IsSuccess) return Result<IReadOnlyList<RedeemableReward>>.Fail(bal.ErrorCode!, bal.ErrorMessage!);
        var points = bal.Value;

        var rewards = await _db.LoyaltyRewards.AsQueryable()
            .Where(r => r.IsActive && (r.ExpiresAtUtc == null || r.ExpiresAtUtc > _clock.UtcNow))
            .OrderBy(r => r.PointsRequired)
            .ToListAsync(ct);

        var list = new List<RedeemableReward>();
        foreach (var r in rewards)
        {
            var valueDisplay = r.RewardType switch
            {
                "FreeDays" => $"{r.ValueMinor} days free",
                "FixedDiscount" => $"{(r.ValueMinor / 100m):N0} KES off",
                "PercentageDiscount" => $"{r.ValueMinor}% off",
                _ => "Custom"
            };
            list.Add(new RedeemableReward(r.Id, r.Name, r.Description, r.PointsRequired, r.RewardType, r.ValueMinor, points >= r.PointsRequired, valueDisplay));
        }
        return Result<IReadOnlyList<RedeemableReward>>.Ok(list);
    }

    public async Task<Result> RedeemAsync(int memberId, int rewardId, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("loyalty.manage")) return Result.Fail("permission.denied", "No permission.");

        var reward = await _db.LoyaltyRewards.AsQueryable().FirstOrDefaultAsync(r => r.Id == rewardId && r.IsActive, ct);
        if (reward is null) return Result.Fail("reward.not_found", "Reward not found or inactive.");

        var bal = await _loyalty.GetBalanceAsync(memberId, ct);
        if (!bal.IsSuccess) return Result.Fail(bal.ErrorCode!, bal.ErrorMessage!);
        if (bal.Value < reward.PointsRequired) return Result.Fail("reward.insufficient_points", $"Member has {bal.Value} points; {reward.PointsRequired} required.");

        // Deduct points
        var input = new LoyaltyEntryInput(memberId, "Redeem", reward.PointsRequired, $"Redeemed: {reward.Name}");
        var entry = await _loyalty.AddEntryAsync(input, ct);
        if (!entry.IsSuccess) return Result.Fail(entry.ErrorCode!, entry.ErrorMessage!);

        // Apply reward benefit
        if (reward.RewardType == "FreeDays")
        {
            var activeMembership = await _db.Memberships.AsQueryable()
                .Where(x => x.MemberId == memberId && x.Status == MembershipStatus.Active)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefaultAsync(ct);

            if (activeMembership is not null)
            {
                activeMembership.EndDate = activeMembership.EndDate.AddDays((int)reward.ValueMinor);
                activeMembership.UpdatedAtUtc = _clock.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (reward.RewardType == "FixedDiscount" || reward.RewardType == "PercentageDiscount")
        {
            // Create a Discount record that can be applied on next purchase
            var discountType = reward.RewardType == "FixedDiscount" ? DiscountType.FixedAmount : DiscountType.Percentage;
            _db.Discounts.Add(new Discount
            {
                Type = discountType,
                Value = reward.ValueMinor,
                Code = $"REWARD-{reward.Id}-{memberId}",
                ValidFrom = _clock.UtcNow,
                ValidTo = _clock.UtcNow.AddDays(90),
                IsActive = true,
                CreatedAtUtc = _clock.UtcNow,
                UpdatedAtUtc = _clock.UtcNow,
                CreatedBy = _user.UserId
            });
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync("loyalty.redeem", "LoyaltyReward", rewardId.ToString(), null, reward.Name, null, ct);
        return Result.Ok();
    }
}
