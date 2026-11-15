namespace FitCore.Application.Payments;

/// <summary>Compact payment view for lists.</summary>
public sealed record PaymentListItem(
    int Id,
    int MemberId,
    long AmountMinor,
    string CurrencyCode,
    string Method,
    string? Reference,
    DateTime ReceivedAtUtc,
    string Status);
