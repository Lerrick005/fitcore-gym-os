using System.Text.Json;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Receipts;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Receipts;

public sealed class ReceiptService : IReceiptService
{
    private readonly FitCoreDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public ReceiptService(FitCoreDbContext db, ISettingsService settings, IClock clock, ICurrentUser user, IAuditLogger audit)
    {
        _db = db;
        _settings = settings;
        _clock = clock;
        _user = user;
        _audit = audit;
    }

    public async Task<Result<Receipt>> GenerateForPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return Result<Receipt>.Fail("payment.not_found", "Payment not found.");

        // Idempotent — if a receipt already exists for this payment, return it
        var existing = await _db.Receipts.FirstOrDefaultAsync(r => r.PaymentId == paymentId, ct);
        if (existing is not null) return Result<Receipt>.Ok(existing);

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == payment.MemberId, ct);
        var membership = payment.MembershipId is null ? null
            : await _db.Memberships.FirstOrDefaultAsync(m => m.Id == payment.MembershipId.Value, ct);

        var branding = await _settings.GetAsync<BrandingSettings>(ct);
        var business = await _settings.GetAsync<BusinessSettings>(ct);
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        var receiptSettings = await _settings.GetAsync<ReceiptSettings>(ct);
        var installation = await _db.Installations.AsQueryable().FirstOrDefaultAsync(ct);

        var receiptNumber = await GenerateReceiptNumberAsync(receiptSettings, ct);
        var now = _clock.UtcNow;

        var snapshot = new
        {
            business = new
            {
                name = branding.BusinessName,
                tradingName = branding.TradingName,
                tagline = branding.Tagline,
                phone = business.PhonePrimary,
                email = business.Email,
                address = business.PhysicalAddress,
                registrationNumber = business.RegistrationNumber,
                taxNumber = business.TaxNumber
            },
            member = member is null ? null : new
            {
                number = member.MemberNumber,
                fullName = member.FullName,
                phone = member.Phone,
                email = member.Email
            },
            membership = membership is null ? null : new
            {
                planName = membership.PlanNameSnapshot,
                startDate = membership.StartDate,
                endDate = membership.EndDate,
                priceMinor = membership.PriceMinorSnapshot,
                currencyCode = membership.CurrencyCodeSnapshot
            },
            payment = new
            {
                amountMinor = payment.AmountMinor,
                currencyCode = payment.CurrencyCode,
                method = payment.Method,
                reference = payment.Reference,
                receivedAtUtc = payment.ReceivedAtUtc
            },
            receipt = new
            {
                header = receiptSettings.HeaderText,
                footer = receiptSettings.FooterText,
                prefix = receiptSettings.ReceiptNumberPrefix,
                showTaxLine = receiptSettings.ShowTaxLine,
                currencySymbol = locale.CurrencySymbol,
                currencyCode = locale.CurrencyCode,
                dateFormat = locale.DateFormat,
                timeFormat = locale.TimeFormat,
                installationId = installation?.Id.ToString()
            }
        };

        var receipt = new Receipt
        {
            ReceiptNumber = receiptNumber,
            PaymentId = paymentId,
            IssuedAtUtc = now,
            IssuedBy = _user.UserId ?? 0,
            SnapshotJson = JsonSerializer.Serialize(snapshot)
        };

        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("receipt.issue", "Receipt", receipt.Id.ToString(),
            null, receiptNumber, null, ct);

        return Result<Receipt>.Ok(receipt);
    }

    public async Task<Result<Receipt>> GetAsync(int receiptId, CancellationToken ct = default)
    {
        var r = await _db.Receipts.FirstOrDefaultAsync(x => x.Id == receiptId, ct);
        return r is null ? Result<Receipt>.Fail("receipt.not_found", "Receipt not found.") : Result<Receipt>.Ok(r);
    }

    public async Task<Result<Receipt>> GetForPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        var r = await _db.Receipts.FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);
        return r is null ? Result<Receipt>.Fail("receipt.not_found", "Receipt not found.") : Result<Receipt>.Ok(r);
    }

    private async Task<string> GenerateReceiptNumberAsync(ReceiptSettings receiptSettings, CancellationToken ct)
    {
        var prefix = string.IsNullOrWhiteSpace(receiptSettings.ReceiptNumberPrefix) ? "R-" : receiptSettings.ReceiptNumberPrefix;
        var counter = await _db.SequenceCounters.FirstOrDefaultAsync(c => c.Name == "receipt", ct);
        if (counter is null)
        {
            counter = new SequenceCounter { Name = "receipt", Prefix = prefix, NextValue = 1 };
            _db.SequenceCounters.Add(counter);
            await _db.SaveChangesAsync(ct);
        }
        var number = $"{counter.Prefix}{counter.NextValue:D6}";
        counter.NextValue += 1;
        await _db.SaveChangesAsync(ct);
        return number;
    }
}
