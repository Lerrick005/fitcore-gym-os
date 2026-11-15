using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Exporting;
using FitCore.Application.Reports;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed class ReportsExpiringRow
{
    public string FullName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public string EndDateDisplay { get; init; } = string.Empty;
    public int DaysLeft { get; init; }
}

public sealed class RevenueRow
{
    public string Method { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed partial class ReportsViewModel : PageViewModelBase
{
    private readonly IReportsService _reports;
    private readonly IReportExporter _exporter;
    private readonly ICurrencyFormatter _money;
    private readonly IDateTimeFormatter _dates;
    private readonly IClock _clock;

    [ObservableProperty] private string _revenueLast30DaysDisplay = string.Empty;
    [ObservableProperty] private int _newMembersLast30Days;
    [ObservableProperty] private int _activeMembers;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;
    private FitCore.Application.Reports.ReportsData? _lastData;

    public ObservableCollection<ReportsExpiringRow> ExpiringRows { get; } = new();

    // Chart series
    public LiveChartsCore.SkiaSharpView.Axis[] RevenueChartXAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.Axis[] RevenueChartYAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.LineSeries<double>[] RevenueChartSeries { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.LineSeries<double>>();

    public LiveChartsCore.SkiaSharpView.Axis[] GrowthChartXAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.Axis[] GrowthChartYAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.ColumnSeries<int>[] GrowthChartSeries { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.ColumnSeries<int>>();

    public LiveChartsCore.SkiaSharpView.Axis[] MonthlyRevenueChartXAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.Axis[] MonthlyRevenueChartYAxes { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.ColumnSeries<double>[] MonthlyRevenueChartSeries { get; private set; } = Array.Empty<LiveChartsCore.SkiaSharpView.ColumnSeries<double>>();

    [ObservableProperty] private string _revenueYtdDisplay = string.Empty;
    [ObservableProperty] private string _revenueThisMonthDisplay = string.Empty;
    [ObservableProperty] private string _revenueLastMonthDisplay = string.Empty;
    [ObservableProperty] private string _revenueMonthlyDelta = string.Empty;
    [ObservableProperty] private bool _revenueIsUp;
    [ObservableProperty] private string _membersThisMonthDelta = string.Empty;
    [ObservableProperty] private bool _membersIsUp;
    public ObservableCollection<RevenueRow> RevenueByMethodRows { get; } = new();

    public override string Title => "Reports";

    public ReportsViewModel(IReportsService reports, ICurrencyFormatter money, IDateTimeFormatter dates, IClock clock, IReportExporter exporter)
    {
        _reports = reports;
        _money = money;
        _dates = dates;
        _clock = clock;
        _exporter = exporter;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var r = await _reports.GetSnapshotAsync(ct);
            if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
            var d = r.Value!;
            _lastData = d;

            var currency = d.RevenueLast30Days.FirstOrDefault()?.CurrencyCode ?? "KES";
            RevenueLast30DaysDisplay = await _money.FormatAsync(d.RevenueLast30DaysTotal, currency, ct);
            NewMembersLast30Days = d.NewMembersLast30Days;
            ActiveMembers = d.ActiveMembers;

            ExpiringRows.Clear();
            foreach (var e in d.ExpiringInNext30Days)
            {
                var daysLeft = (int)(e.EndDate.Date - _clock.UtcNow.Date).TotalDays;
                var endDisplay = await _dates.FormatDateAsync(e.EndDate, ct);
                ExpiringRows.Add(new ReportsExpiringRow
                {
                    FullName = e.FullName,
                    MemberNumber = e.MemberNumber,
                    PlanName = e.PlanName,
                    EndDateDisplay = endDisplay,
                    DaysLeft = daysLeft
                });
            }

            RevenueByMethodRows.Clear();
            foreach (var m in d.RevenueByMethodLast30Days)
            {
                var amount = await _money.FormatAsync(m.AmountMinor, currency, ct);
                RevenueByMethodRows.Add(new RevenueRow
                {
                    Method = m.Method,
                    AmountDisplay = amount,
                    Count = m.Count
                });
            }

            // YTD / monthly displays
            RevenueYtdDisplay = await _money.FormatAsync(d.RevenueYearToDate, currency, ct);
            RevenueThisMonthDisplay = await _money.FormatAsync(d.RevenueThisMonth, currency, ct);
            RevenueLastMonthDisplay = await _money.FormatAsync(d.RevenueLastMonth, currency, ct);

            if (d.RevenueLastMonth > 0)
            {
                var delta = ((d.RevenueThisMonth - d.RevenueLastMonth) * 100.0 / d.RevenueLastMonth);
                RevenueMonthlyDelta = $"{(delta >= 0 ? "+" : "")}{delta:F1}% vs last month";
                RevenueIsUp = delta >= 0;
            }
            else
            {
                RevenueMonthlyDelta = d.RevenueThisMonth > 0 ? "First revenue month" : "No revenue yet";
                RevenueIsUp = d.RevenueThisMonth > 0;
            }

            var newMembersThisMonth = d.MemberGrowthLast12Months
                .FirstOrDefault(m => m.Year == _clock.UtcNow.Year && m.Month == _clock.UtcNow.Month)?.NewMembers ?? 0;
            MembersThisMonthDelta = $"{newMembersThisMonth} new this month";
            MembersIsUp = newMembersThisMonth > 0;

            // ===== Build charts =====
            var brandPrimary = SkiaSharp.SKColor.Parse("#B8451F");
            var brandSecondary = SkiaSharp.SKColor.Parse("#6B7280");

            // Daily revenue (30 days) — Line
            var revenueValues = d.RevenueLast30Days
                .OrderBy(r => r.Date)
                .Select(r => (double)(r.AmountMinor / 100m))
                .ToArray();
            var revenueLabels = d.RevenueLast30Days
                .OrderBy(r => r.Date)
                .Select(r => r.Date.ToString("dd/MM"))
                .ToArray();

            RevenueChartSeries = new[]
            {
                new LiveChartsCore.SkiaSharpView.LineSeries<double>
                {
                    Values = revenueValues,
                    Stroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(brandPrimary) { StrokeThickness = 2.5f },
                    Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(brandPrimary.WithAlpha(30)),
                    GeometrySize = 6,
                    Name = "Revenue"
                }
            };
            RevenueChartXAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = revenueLabels,
                    LabelsRotation = 0,
                    TextSize = 10,
                    SeparatorsPaint = null
                }
            };
            RevenueChartYAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labeler = v => $"{(v / 1000):F0}k",
                    TextSize = 10,
                    SeparatorsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse("#E8E1D2")) { StrokeThickness = 1 }
                }
            };

            // Daily member growth (30 days) — Column
            var growthValues = d.MemberGrowthLast30Days
                .OrderBy(g => g.Date)
                .Select(g => g.NewMembers)
                .ToArray();
            var growthLabels = d.MemberGrowthLast30Days
                .OrderBy(g => g.Date)
                .Select(g => g.Date.ToString("dd/MM"))
                .ToArray();

            GrowthChartSeries = new[]
            {
                new LiveChartsCore.SkiaSharpView.ColumnSeries<int>
                {
                    Values = growthValues,
                    Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(brandPrimary),
                    Name = "New members"
                }
            };
            GrowthChartXAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = growthLabels,
                    LabelsRotation = 0,
                    TextSize = 10,
                    SeparatorsPaint = null
                }
            };
            GrowthChartYAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    MinLimit = 0,
                    TextSize = 10,
                    SeparatorsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse("#E8E1D2")) { StrokeThickness = 1 }
                }
            };

            // Monthly revenue (12 months) — Column
            var monthlyRevenue = d.RevenueLast12Months
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .Select(r => (double)(r.AmountMinor / 100m))
                .ToArray();
            var monthlyLabels = d.RevenueLast12Months
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .Select(r => new DateTime(r.Year, r.Month, 1).ToString("MMM yy"))
                .ToArray();

            MonthlyRevenueChartSeries = new[]
            {
                new LiveChartsCore.SkiaSharpView.ColumnSeries<double>
                {
                    Values = monthlyRevenue,
                    Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(brandPrimary),
                    Name = "Monthly revenue"
                }
            };
            MonthlyRevenueChartXAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = monthlyLabels,
                    LabelsRotation = 30,
                    TextSize = 10,
                    SeparatorsPaint = null
                }
            };
            MonthlyRevenueChartYAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labeler = v => $"{(v / 1000):F0}k",
                    TextSize = 10,
                    SeparatorsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse("#E8E1D2")) { StrokeThickness = 1 }
                }
            };
        }
        finally { IsBusy = false; }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (_lastData is null) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"fitcore-report-{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        var r = await _exporter.ExportReportsToCsvAsync(_lastData, dlg.FileName);
        StatusMessage = r.IsSuccess ? $"Exported: {dlg.FileName}" : r.ErrorMessage;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (_lastData is null) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"fitcore-report-{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dlg.ShowDialog() != true) return;
        var r = await _exporter.ExportReportsToPdfAsync(_lastData, dlg.FileName);
        StatusMessage = r.IsSuccess ? $"Exported: {dlg.FileName}" : r.ErrorMessage;
    }
}



