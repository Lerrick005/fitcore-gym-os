using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Plans;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Plans;

public sealed class MembershipPlanService : IMembershipPlanService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public MembershipPlanService(FitCoreDbContext db, IClock clock, ICurrentUser user, IAuditLogger audit)
    {
        _db = db;
        _clock = clock;
        _user = user;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<MembershipPlanListItem>>> GetActiveAsync(CancellationToken ct = default)
    {
        await EnsurePlansSeededAsync(ct);
        return await QueryAsync(activeOnly: true, ct);
    }

    public async Task<Result<IReadOnlyList<MembershipPlanListItem>>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsurePlansSeededAsync(ct);
        return await QueryAsync(activeOnly: false, ct);
    }

    private async Task<Result<IReadOnlyList<MembershipPlanListItem>>> QueryAsync(bool activeOnly, CancellationToken ct)
    {
        var q = _db.MembershipPlans.AsQueryable();
        if (activeOnly) q = q.Where(p => p.IsActive);

        var plans = await q
            .OrderBy(p => p.Category).ThenBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new MembershipPlanListItem(p.Id, p.Name, p.PriceMinor, p.CurrencyCode,
                p.DurationValue, p.DurationUnit.ToString(), p.Category))
            .ToListAsync(ct);

        return Result<IReadOnlyList<MembershipPlanListItem>>.Ok(plans);
    }

    public async Task<Result<MembershipPlan>> GetAsync(int planId, CancellationToken ct = default)
    {
        await EnsurePlansSeededAsync(ct);
        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        return plan is null ? Result<MembershipPlan>.Fail("plan.not_found", "Plan not found.") : Result<MembershipPlan>.Ok(plan);
    }

    public async Task<Result<MembershipPlan>> CreateAsync(PlanInput input, CancellationToken ct = default)
    {
        if (!_user.HasPermission("settings.manage"))
            return Result<MembershipPlan>.Fail("permission.denied", "You do not have permission to manage plans.");

        if (string.IsNullOrWhiteSpace(input.Name))
            return Result<MembershipPlan>.Fail("validation.name", "Plan name is required.");
        if (input.PriceMinor < 0)
            return Result<MembershipPlan>.Fail("validation.price", "Price cannot be negative.");
        if (input.DurationValue <= 0)
            return Result<MembershipPlan>.Fail("validation.duration", "Duration must be positive.");

        var du = Enum.TryParse<DurationUnit>(input.DurationUnit, true, out var parsed) ? parsed : DurationUnit.Months;
        var now = _clock.UtcNow;
        var plan = new MembershipPlan
        {
            Name = input.Name.Trim(),
            Description = input.Description,
            PriceMinor = input.PriceMinor,
            CurrencyCode = "KES",
            DurationValue = input.DurationValue,
            DurationUnit = du,
            Category = string.IsNullOrWhiteSpace(input.Category) ? "Membership" : input.Category,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
            IsDefault = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = _user.UserId
        };
        _db.MembershipPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("plan.create", "MembershipPlan", plan.Id.ToString(), null, plan.Name, null, ct);
        return Result<MembershipPlan>.Ok(plan);
    }

    public async Task<Result<MembershipPlan>> UpdateAsync(int planId, PlanInput input, CancellationToken ct = default)
    {
        if (!_user.HasPermission("settings.manage"))
            return Result<MembershipPlan>.Fail("permission.denied", "You do not have permission to manage plans.");

        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return Result<MembershipPlan>.Fail("plan.not_found", "Plan not found.");

        if (string.IsNullOrWhiteSpace(input.Name))
            return Result<MembershipPlan>.Fail("validation.name", "Plan name is required.");

        var du = Enum.TryParse<DurationUnit>(input.DurationUnit, true, out var parsed) ? parsed : plan.DurationUnit;
        plan.Name = input.Name.Trim();
        plan.Description = input.Description;
        plan.PriceMinor = input.PriceMinor;
        plan.DurationValue = input.DurationValue;
        plan.DurationUnit = du;
        plan.Category = input.Category;
        plan.SortOrder = input.SortOrder;
        plan.IsActive = input.IsActive;
        plan.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("plan.update", "MembershipPlan", plan.Id.ToString(), null, plan.Name, null, ct);
        return Result<MembershipPlan>.Ok(plan);
    }

    public async Task<Result> ArchiveAsync(int planId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("settings.manage"))
            return Result.Fail("permission.denied", "You do not have permission to archive plans.");

        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return Result.Fail("plan.not_found", "Plan not found.");

        plan.IsActive = false;
        plan.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("plan.archive", "MembershipPlan", plan.Id.ToString(), null, null, null, ct);
        return Result.Ok();
    }

    public async Task<Result> RestoreAsync(int planId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("settings.manage"))
            return Result.Fail("permission.denied", "You do not have permission to restore plans.");

        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return Result.Fail("plan.not_found", "Plan not found.");

        plan.IsActive = true;
        plan.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("plan.restore", "MembershipPlan", plan.Id.ToString(), null, null, null, ct);
        return Result.Ok();
    }

    private async Task EnsurePlansSeededAsync(CancellationToken ct)
    {
        var hasDefaults = await _db.MembershipPlans.AnyAsync(p => p.IsDefault, ct);
        if (hasDefaults) return;

        var now = _clock.UtcNow;
        foreach (var p in GetDefaultPlans())
        {
            _db.MembershipPlans.Add(new MembershipPlan
            {
                Name = p.Name, Description = p.Description, PriceMinor = p.PriceMinor,
                CurrencyCode = "KES", DurationValue = p.DurationValue, DurationUnit = p.DurationUnit,
                Category = p.Category, IsActive = true, IsDefault = true, SortOrder = p.SortOrder,
                CreatedAtUtc = now, UpdatedAtUtc = now
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private static List<SeedPlan> GetDefaultPlans() => new()
    {
        new SeedPlan("Monthly", "1 month full access", 400000, 1, DurationUnit.Months, "Membership", 0),
        new SeedPlan("3 months", "3 months full access", 1140000, 3, DurationUnit.Months, "Membership", 1),
        new SeedPlan("6 months", "6 months full access", 2220000, 6, DurationUnit.Months, "Membership", 2),
        new SeedPlan("Annual", "12 months full access", 4200000, 12, DurationUnit.Months, "Membership", 3),
        new SeedPlan("Daily pass", "One-day access", 30000, 1, DurationUnit.Days, "Pass", 0),
        new SeedPlan("Weekly", "7 days full access", 130000, 1, DurationUnit.Weeks, "Pass", 1),
        new SeedPlan("Personal training", "One-on-one session", 550000, 1, DurationUnit.Months, "Service", 0),
        new SeedPlan("Nutrition support", "Monthly nutrition guidance", 100000, 1, DurationUnit.Months, "Service", 1),
        new SeedPlan("Team building", "Per head", 80000, 1, DurationUnit.Days, "Service", 2),
        new SeedPlan("Corporate package", "10-pax package for 3 months", 5000000, 3, DurationUnit.Months, "Service", 3)
    };

    private sealed record SeedPlan(string Name, string Description, long PriceMinor, int DurationValue, DurationUnit DurationUnit, string Category, int SortOrder);
}
