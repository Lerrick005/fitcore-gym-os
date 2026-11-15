using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Payments;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed class PaymentRow
{
    public int Id { get; init; }
    public int MemberId { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public string MemberNumber { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public string ReceivedAtDisplay { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed partial class PaymentsViewModel : PageViewModelBase
{
    private readonly IPaymentService _payments;
    private readonly ICurrencyFormatter _money;
    private readonly IDateTimeFormatter _dates;
    private readonly FitCore.Infrastructure.Persistence.FitCoreDbContext _db;

    [ObservableProperty] private ObservableCollection<PaymentRow> _results = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _methodFilter = "All";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasResults;

    public string[] Methods { get; } = new[] { "All", "Cash", "M-Pesa", "Card", "Bank transfer", "Other" };

    public event EventHandler<int>? ShowReceiptRequested;

    public override string Title => "Payments";

    public PaymentsViewModel(
        IPaymentService payments,
        ICurrencyFormatter money,
        IDateTimeFormatter dates,
        FitCore.Infrastructure.Persistence.FitCoreDbContext db)
    {
        _payments = payments;
        _money = money;
        _dates = dates;
        _db = db;
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
            var query = new PaymentListQuery
            {
                Text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                Method = MethodFilter == "All" ? null : MethodFilter,
                Take = 200
            };

            var result = await _payments.ListAllAsync(query, ct);
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage;
                Results.Clear();
                HasResults = false;
                return;
            }

            // Join member names
            var memberIds = result.Value!.Select(p => p.MemberId).Distinct().ToList();
            var members = await _db.Members
                .AsQueryable()
                .Where(m => memberIds.Contains(m.Id))
                .Select(m => new { m.Id, m.FullName, m.MemberNumber })
                .ToListAsync(ct);
            var memberMap = members.ToDictionary(m => m.Id);

            Results.Clear();
            foreach (var p in result.Value!)
            {
                var amount = await _money.FormatAsync(p.AmountMinor, p.CurrencyCode, ct);
                var date = await _dates.FormatDateTimeAsync(p.ReceivedAtUtc, ct);
                memberMap.TryGetValue(p.MemberId, out var m);
                Results.Add(new PaymentRow
                {
                    Id = p.Id,
                    MemberId = p.MemberId,
                    MemberName = m?.FullName ?? "(unknown)",
                    MemberNumber = m?.MemberNumber ?? "",
                    AmountDisplay = amount,
                    Method = p.Method,
                    Reference = p.Reference,
                    ReceivedAtUtc = p.ReceivedAtUtc,
                    ReceivedAtDisplay = date,
                    Status = p.Status
                });
            }
            HasResults = Results.Count > 0;
        }
        finally { IsBusy = false; }
    }

    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();
    partial void OnMethodFilterChanged(string value) => _ = RefreshAsync();

    [RelayCommand]
    private void ShowReceipt(PaymentRow? row)
    {
        if (row is null) return;
        ShowReceiptRequested?.Invoke(this, row.Id);
    }
}
