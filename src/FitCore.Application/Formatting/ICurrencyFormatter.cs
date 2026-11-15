using FitCore.Domain.ValueObjects;

namespace FitCore.Application.Formatting;

/// <summary>Currency display formatting that respects configured locale (PRS §8.1).</summary>
public interface ICurrencyFormatter
{
    Task<string> FormatAsync(Money money, CancellationToken ct = default);
    Task<string> FormatAsync(long amountMinor, string currencyCode, CancellationToken ct = default);
}
