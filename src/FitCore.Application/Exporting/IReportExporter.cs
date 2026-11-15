using FitCore.Application.Common;
using FitCore.Application.Reports;

namespace FitCore.Application.Exporting;

public interface IReportExporter
{
    /// <summary>Export the reports snapshot as CSV (Excel-compatible).</summary>
    Task<Result<string>> ExportReportsToCsvAsync(ReportsData data, string destinationPath, CancellationToken ct = default);

    /// <summary>Export the reports snapshot as PDF.</summary>
    Task<Result<string>> ExportReportsToPdfAsync(ReportsData data, string destinationPath, CancellationToken ct = default);

    /// <summary>Export the members list as CSV.</summary>
    Task<Result<string>> ExportMembersToCsvAsync(IReadOnlyList<FitCore.Application.Members.MemberListItem> members, string destinationPath, CancellationToken ct = default);

    /// <summary>Export the payments list as CSV.</summary>
    Task<Result<string>> ExportPaymentsToCsvAsync(IReadOnlyList<FitCore.Application.Payments.PaymentListItem> payments, string destinationPath, CancellationToken ct = default);
}
