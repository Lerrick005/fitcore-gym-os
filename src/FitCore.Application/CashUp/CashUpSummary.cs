namespace FitCore.Application.CashUp;

public sealed record CashUpMethodLine(string Method, int Count, long TotalMinor);
public sealed record CashUpSummary(
    DateTime Date,
    long TotalRevenueMinor,
    string CurrencyCode,
    IReadOnlyList<CashUpMethodLine> ByMethod,
    IReadOnlyList<CashUpReceiptLine> Receipts);
public sealed record CashUpReceiptLine(DateTime TimeUtc, string MemberName, string MemberNumber, string Method, long AmountMinor, string? Reference);
