using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Plans;

public interface IMembershipPlanService
{
    Task<Result<IReadOnlyList<MembershipPlanListItem>>> GetActiveAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<MembershipPlanListItem>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<MembershipPlan>> GetAsync(int planId, CancellationToken ct = default);

    Task<Result<MembershipPlan>> CreateAsync(PlanInput input, CancellationToken ct = default);
    Task<Result<MembershipPlan>> UpdateAsync(int planId, PlanInput input, CancellationToken ct = default);
    Task<Result> ArchiveAsync(int planId, CancellationToken ct = default);
    Task<Result> RestoreAsync(int planId, CancellationToken ct = default);
}

/// <summary>Input for creating or editing a plan.</summary>
public sealed record PlanInput(
    string Name,
    string? Description,
    long PriceMinor,
    int DurationValue,
    string DurationUnit,   // "Days", "Weeks", "Months"
    string Category,       // "Membership", "Pass", "Service"
    int SortOrder,
    bool IsActive);
