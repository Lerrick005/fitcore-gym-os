namespace FitCore.Application.Plans;

/// <summary>Compact plan view for pickers and lists.</summary>
public sealed record MembershipPlanListItem(
    int Id,
    string Name,
    long PriceMinor,
    string CurrencyCode,
    int DurationValue,
    string DurationUnit,
    string Category);
