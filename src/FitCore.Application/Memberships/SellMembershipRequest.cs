namespace FitCore.Application.Memberships;

/// <summary>Request to sell a membership (PRS §12.2).</summary>
public sealed record SellMembershipRequest(
    int MemberId,
    int PlanId,
    DateTime? StartDate,     // null = today
    int? DiscountId);
