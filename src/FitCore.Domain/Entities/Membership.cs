using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class Membership : AuditableEntity
{
    public int MemberId { get; set; }
    public int PlanId { get; set; }

    // Snapshotted at purchase (PRS §8.3, §13.3). Editing the plan never changes these.
    public string PlanNameSnapshot { get; set; } = string.Empty;
    public long PriceMinorSnapshot { get; set; }
    public string CurrencyCodeSnapshot { get; set; } = string.Empty;
    public int DurationSnapshot { get; set; }
    public string? TermsSnapshotJson { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public MembershipStatus Status { get; set; }
    public int? DiscountId { get; set; }
    public DateTime? FrozenFrom { get; set; }
    public DateTime? FrozenTo { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}
