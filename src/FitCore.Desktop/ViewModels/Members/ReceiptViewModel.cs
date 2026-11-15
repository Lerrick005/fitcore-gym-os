using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Receipts;
using FitCore.Domain.Entities;

namespace FitCore.Desktop.ViewModels.Members;

public sealed partial class ReceiptViewModel : ObservableObject
{
    private readonly IReceiptService _receipts;

    [ObservableProperty] private int _receiptId;
    [ObservableProperty] private string _receiptNumber = string.Empty;
    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _businessContact = string.Empty;
    [ObservableProperty] private string _businessAddress = string.Empty;
    [ObservableProperty] private string _taxNumber = string.Empty;
    [ObservableProperty] private string _memberName = string.Empty;
    [ObservableProperty] private string _memberNumber = string.Empty;
    [ObservableProperty] private string _planName = string.Empty;
    [ObservableProperty] private string _planDates = string.Empty;
    [ObservableProperty] private string _amountDisplay = string.Empty;
    [ObservableProperty] private string _method = string.Empty;
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _issuedAt = string.Empty;
    [ObservableProperty] private string _headerText = string.Empty;
    [ObservableProperty] private string _footerText = string.Empty;
    [ObservableProperty] private bool _showTaxLine;

    public event EventHandler? PrintRequested;

    [ObservableProperty] private bool _isDuplicate;
    [ObservableProperty] private string _paperType = "thermal80";
    [ObservableProperty] private double _receiptWidth = 280;  // default thermal 58mm ~ 220, 80mm ~ 300
    [ObservableProperty] private double _receiptFontSize = 10;

    public ReceiptViewModel(IReceiptService receipts) => _receipts = receipts;

    public async Task InitializeForPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        var r = await _receipts.GetForPaymentAsync(paymentId, ct);
        if (r.IsSuccess) await LoadSnapshotAsync(r.Value!, ct);
    }

    public async Task InitializeAsync(int receiptId, CancellationToken ct = default)
    {
        var r = await _receipts.GetAsync(receiptId, ct);
        if (r.IsSuccess) await LoadSnapshotAsync(r.Value!, ct);
    }

    private Task LoadSnapshotAsync(Receipt receipt, CancellationToken ct)
    {
        ReceiptId = receipt.Id;
        ReceiptNumber = receipt.ReceiptNumber;
        IssuedAt = receipt.IssuedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        using var doc = JsonDocument.Parse(receipt.SnapshotJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("business", out var biz))
        {
            BusinessName = biz.GetProperty("name").GetString() ?? string.Empty;
            var phone = biz.TryGetProperty("phone", out var p) ? p.GetString() : null;
            var email = biz.TryGetProperty("email", out var e) ? e.GetString() : null;
            BusinessContact = string.Join("  •  ", new[] { phone, email }.Where(s => !string.IsNullOrWhiteSpace(s)));
            BusinessAddress = biz.TryGetProperty("address", out var a) ? a.GetString() ?? string.Empty : string.Empty;
            TaxNumber = biz.TryGetProperty("taxNumber", out var t) ? t.GetString() ?? string.Empty : string.Empty;
        }

        if (root.TryGetProperty("member", out var mbr) && mbr.ValueKind != JsonValueKind.Null)
        {
            MemberName = mbr.GetProperty("fullName").GetString() ?? string.Empty;
            MemberNumber = mbr.GetProperty("number").GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("membership", out var mem) && mem.ValueKind != JsonValueKind.Null)
        {
            PlanName = mem.GetProperty("planName").GetString() ?? string.Empty;
            var start = mem.GetProperty("startDate").GetDateTime();
            var end = mem.GetProperty("endDate").GetDateTime();
            PlanDates = $"{start:dd/MM/yyyy} → {end:dd/MM/yyyy}";
        }

        if (root.TryGetProperty("payment", out var pay))
        {
            var amount = pay.GetProperty("amountMinor").GetInt64();
            var currency = pay.GetProperty("currencyCode").GetString();
            var locale = root.GetProperty("receipt");
            var sym = locale.TryGetProperty("currencySymbol", out var s) ? s.GetString() : "KES";
            AmountDisplay = $"{sym} {amount / 100m:N2}";
            Method = pay.GetProperty("method").GetString() ?? string.Empty;
            Reference = pay.TryGetProperty("reference", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() ?? string.Empty : string.Empty;
        }

        if (root.TryGetProperty("receipt", out var rc))
        {
            HeaderText = rc.TryGetProperty("header", out var h) ? h.GetString() ?? string.Empty : string.Empty;
            PaperType = rc.TryGetProperty("paper", out var pt) ? pt.GetString() ?? "thermal80" : "thermal80";
            switch (PaperType)
            {
                case "thermal58": ReceiptWidth = 220; ReceiptFontSize = 9; break;
                case "thermal80": ReceiptWidth = 300; ReceiptFontSize = 10; break;
                default: ReceiptWidth = 480; ReceiptFontSize = 11; break;  // A4-ish
            }
            FooterText = rc.TryGetProperty("footer", out var f) ? f.GetString() ?? string.Empty : string.Empty;
            ShowTaxLine = rc.TryGetProperty("showTaxLine", out var t) && t.GetBoolean();
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Print() => PrintRequested?.Invoke(this, EventArgs.Empty);
}

