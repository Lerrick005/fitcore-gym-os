using FitCore.Application.Common;

namespace FitCore.Application.Reports;

public interface IReportsService
{
    Task<Result<ReportsData>> GetSnapshotAsync(CancellationToken ct = default);
}
