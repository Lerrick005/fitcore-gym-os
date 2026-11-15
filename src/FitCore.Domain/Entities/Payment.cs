using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class Payment : AuditableEntity
{
    public int MemberId { get; set; }
    public int? MembershipId { get; set; }
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public int ReceivedBy { get; set; }
    public PaymentStatus Status { get; set; }
    public int? RelatedPaymentId { get; set; }
}
