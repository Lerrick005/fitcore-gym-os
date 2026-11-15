namespace FitCore.Application.Dashboard;

public interface IDashboardService
{
    /// <summary>Reads current counts for the dashboard. Returns zeroes on empty DB.</summary>
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
}
