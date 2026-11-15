using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.CashUp;
using FitCore.Application.Exporting;
using FitCore.Application.Formatting;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed class CashUpMethodRow
{
    public string Method { get; init; } = string.Empty;
    public int Count { get; init; }
    public string TotalDisplay { get; init; } = string.Empty;
}

public sealed class CashUpReceiptRow
{
    public string TimeDisplay { get; init; } = string.Empty;
    public string MemberName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string? Reference { get; init; }
}

public sealed partial class CashUpViewModel : PageViewModelBase
{
    private readonly ICashUpService _cashUp;
    private readonly ICurrencyFormatter _money;
    private readonly IDateTimeFormatter _dates;
    private readonly IClock _clock;
    private CashUpSummary? _lastSummary;

    public ObservableCollection<CashUpMethodRow> ByMethod { get; } = new();
    public ObservableCollection<CashUpReceiptRow> Receipts { get; } = new();

    [ObservableProperty] private DateTime? _selectedDate = DateTime.UtcNow.Date;
    [ObservableProperty] private string _totalDisplay = "0.00";
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;

    public override string Title => "Cash-up";

    public CashUpViewModel(ICashUpService cashUp, ICurrencyFormatter money, IDateTimeFormatter dates, IClock clock)
    {
        _cashUp = cashUp;
        _money = money;
        _dates = dates;
        _clock = clock;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var date = SelectedDate ?? _clock.UtcNow.Date;
            var r = await _cashUp.GetDayAsync(date, ct);
            if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
            var d = r.Value!;
            _lastSummary = d;

            TotalDisplay = await _money.FormatAsync(d.TotalRevenueMinor, d.CurrencyCode, ct);

            ByMethod.Clear();
            foreach (var m in d.ByMethod)
            {
                ByMethod.Add(new CashUpMethodRow
                {
                    Method = m.Method,
                    Count = m.Count,
                    TotalDisplay = await _money.FormatAsync(m.TotalMinor, d.CurrencyCode, ct)
                });
            }

            Receipts.Clear();
            foreach (var rcp in d.Receipts)
            {
                var time = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(rcp.TimeUtc, DateTimeKind.Utc),
                    TimeZoneInfo.Local);
                Receipts.Add(new CashUpReceiptRow
                {
                    TimeDisplay = time.ToString("HH:mm"),
                    MemberName = rcp.MemberName,
                    MemberNumber = rcp.MemberNumber,
                    Method = rcp.Method,
                    AmountDisplay = await _money.FormatAsync(rcp.AmountMinor, d.CurrencyCode, ct),
                    Reference = rcp.Reference
                });
            }
        }
        finally { IsBusy = false; }
    }

    partial void OnSelectedDateChanged(DateTime? value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshCommandAsync() => await RefreshAsync();

    [RelayCommand]
    private void Print()
    {
        var dateStr = (SelectedDate ?? DateTime.UtcNow.Date).ToString("dd/MM/yyyy");
        var lines = new List<string>
        {
            $"CASH-UP — {dateStr}",
            $"Total: {TotalDisplay}",
            "",
            "BY METHOD:"
        };
        foreach (var m in ByMethod)
            lines.Add($"  {m.Method}: {m.Count} payments, {m.TotalDisplay}");
        lines.Add("");
        lines.Add("RECEIPTS:");
        foreach (var r in Receipts)
            lines.Add($"  {r.TimeDisplay}  {r.MemberName}  {r.Method}  {r.AmountDisplay}");

        var content = string.Join(Environment.NewLine, lines);
        var result = System.Windows.MessageBox.Show(content + "\n\nCopy to clipboard?",
            "Cash-up summary", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
        if (result == System.Windows.MessageBoxResult.Yes)
            System.Windows.Clipboard.SetText(content);
    }
}

