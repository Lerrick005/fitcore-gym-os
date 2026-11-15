using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Memberships;

/// <summary>
/// Membership lifecycle (PRS §12.2). Each membership snapshots its plan at purchase;
/// editing a plan never changes existing memberships.
/// </summary>
public interface IMembershipService
{
    /// <summary>
    /// Sell a plan to a member. Snapshots plan price/duration/terms.
    /// Blocked if the member already has an active membership.
    /// </summary>
    Task<Result<Membership>> SellAsync(SellMembershipRequest request, CancellationToken ct = default);

    /// <summary>Freeze an active membership within configured limits.</summary>
    Task<Result<Membership>> FreezeAsync(int membershipId, DateTime frozenFrom, DateTime frozenTo, string? reason, CancellationToken ct = default);

    /// <summary>Unfreeze a frozen membership.</summary>
    Task<Result<Membership>> UnfreezeAsync(int membershipId, CancellationToken ct = default);

    /// <summary>Cancel an active or frozen membership.</summary>
    Task<Result> CancelAsync(int membershipId, string? reason, CancellationToken ct = default);

    /// <summary>Get the current active membership for a member, if any. Auto-expires if past EndDate.</summary>
    Task<Result<Membership>> GetActiveForMemberAsync(int memberId, CancellationToken ct = default);

    /// <summary>List all memberships for a member, newest first.</summary>
    Task<Result<IReadOnlyList<MembershipListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default);
}
