namespace FitCore.Application.Dashboard;

/// <summary>
/// Aggregate numbers shown on the dashboard (PRS §12.5).
/// All counts are "right now" — cheap queries against indexed columns.
/// </summary>
public sealed record DashboardStats(
    int ActiveMembers,
    int ExpiringSoonMembers,
    int ExpiredMembers,
    int TodayCheckIns,
    long TodayRevenueMinor,
    string CurrencyCode,
    int OutstandingBalancesCount,
    int TotalMembers);
