using FitCore.Application.Common;

namespace FitCore.Application.CashUp;

public interface ICashUpService
{
    /// <summary>Summary of payments for a given business day (in configured timezone).</summary>
    Task<Result<CashUpSummary>> GetDayAsync(DateTime date, CancellationToken ct = default);
}
