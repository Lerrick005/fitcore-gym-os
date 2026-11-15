using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class LoyaltyLedgerEntry : AuditableEntity
{
    public int MemberId { get; set; }
    public LoyaltyEntryType Type { get; set; }
    public int Points { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Reason { get; set; }
}
