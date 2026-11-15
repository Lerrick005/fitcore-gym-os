using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class MembershipPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PriceMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int DurationValue { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public int? FreezeAllowanceDays { get; set; }
    public int? VisitLimit { get; set; }
    public long? JoiningFeeMinor { get; set; }
    public string? JoiningFeeCurrencyCode { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Category: "Membership", "Pass", "Service". Used for grouping in the sell dialog.</summary>
    public string Category { get; set; } = "Membership";

    /// <summary>Marks plans seeded from the vendor defaults, so the owner can distinguish them from custom plans.</summary>
    public bool IsDefault { get; set; }
}
