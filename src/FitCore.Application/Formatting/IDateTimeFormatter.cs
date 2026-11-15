namespace FitCore.Application.Formatting;

/// <summary>Date/time display formatting that respects configured time zone (PRS §8.1).</summary>
public interface IDateTimeFormatter
{
    /// <summary>Convert UTC instant to configured zone and format using configured date format.</summary>
    Task<string> FormatDateAsync(DateTime utcInstant, CancellationToken ct = default);

    /// <summary>Convert UTC instant to configured zone and format using configured time format.</summary>
    Task<string> FormatTimeAsync(DateTime utcInstant, CancellationToken ct = default);

    /// <summary>Convert UTC instant to configured zone and format date + time.</summary>
    Task<string> FormatDateTimeAsync(DateTime utcInstant, CancellationToken ct = default);

    /// <summary>Get the business day start (UTC) for the given UTC instant in the configured zone.</summary>
    Task<DateTime> GetBusinessDayStartUtcAsync(DateTime utcInstant, CancellationToken ct = default);
}
