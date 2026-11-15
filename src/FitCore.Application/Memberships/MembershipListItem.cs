namespace FitCore.Application.Memberships;

/// <summary>Compact membership view for member detail / lists.</summary>
public sealed record MembershipListItem(
    int Id,
    int MemberId,
    string PlanName,
    long PriceMinor,
    string CurrencyCode,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    DateTime? FrozenFrom,
    DateTime? FrozenTo);
