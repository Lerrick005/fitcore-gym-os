namespace FitCore.Domain.ValueObjects;

/// <summary>
/// Money as integer minor units + currency code (PRS §8.1).
/// Never use float or double. Arithmetic centralised here.
/// </summary>
public readonly record struct Money(long AmountMinor, string CurrencyCode)
{
    public Money Add(Money other)
    {
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.Ordinal))
            throw new InvalidOperationException($"Cannot add {CurrencyCode} and {other.CurrencyCode}.");
        return new Money(AmountMinor + other.AmountMinor, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.Ordinal))
            throw new InvalidOperationException($"Cannot subtract {CurrencyCode} and {other.CurrencyCode}.");
        return new Money(AmountMinor - other.AmountMinor, CurrencyCode);
    }

    public override string ToString() => $"{AmountMinor} {CurrencyCode}";
}
