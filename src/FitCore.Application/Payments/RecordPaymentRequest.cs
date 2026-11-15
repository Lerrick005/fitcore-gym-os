namespace FitCore.Application.Payments;

/// <summary>Request to record a payment.</summary>
public sealed record RecordPaymentRequest(
    int MemberId,
    int? MembershipId,
    long AmountMinor,
    string CurrencyCode,
    string Method,
    string? Reference,
    string? Notes);
