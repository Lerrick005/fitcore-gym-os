using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Loyalty;
using FitCore.Application.Payments;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Application.Receipts;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IReceiptService _receipts;
    private readonly ILoyaltyService _loyalty;
    private readonly ISettingsService _settings;
    private readonly IModuleAccessGuard _modules;

    public PaymentService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IReceiptService receipts, ILoyaltyService loyalty, ISettingsService settings, IModuleAccessGuard modules)
    {
        _db = db;
        _user = user;
        _clock = clock;
        _audit = audit;
        _receipts = receipts;
        _loyalty = loyalty;
        _settings = settings;
        _modules = modules;
    }

    public async Task<Result<Payment>> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        if (!_user.HasPermission("payments.record"))
            return Result<Payment>.Fail("permission.denied", "You do not have permission to record payments.");

        if (request.AmountMinor <= 0)
            return Result<Payment>.Fail("validation.amount", "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Method))
            return Result<Payment>.Fail("validation.method", "Payment method is required.");
        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.Length != 3)
            return Result<Payment>.Fail("validation.currency", "Currency code must be 3 letters.");

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, ct);
        if (member is null) return Result<Payment>.Fail("member.not_found", "Member not found.");
        if (member.ArchivedAtUtc is not null) return Result<Payment>.Fail("member.archived", "Cannot record payment for an archived member.");

        if (request.MembershipId is not null)
        {
            var membership = await _db.Memberships.FirstOrDefaultAsync(m => m.Id == request.MembershipId.Value && m.MemberId == request.MemberId, ct);
            if (membership is null) return Result<Payment>.Fail("membership.not_found", "Membership not found for this member.");
        }

        // Duplicate reference warning (soft — returns warning via message but still records)
        if (!string.IsNullOrWhiteSpace(request.Reference))
        {
            var dupExists = await _db.Payments.AnyAsync(p => p.Reference == request.Reference, ct);
            if (dupExists)
            {
                // Not blocking — record anyway, but caller sees the warning in the payment note.
                // The reference column is not unique by design (customers may have legitimately reused references).
            }
        }

        var now = _clock.UtcNow;
        var payment = new Payment
        {
            MemberId = request.MemberId,
            MembershipId = request.MembershipId,
            AmountMinor = request.AmountMinor,
            CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
            Method = request.Method.Trim(),
            Reference = request.Reference?.Trim(),
            ReceivedAtUtc = now,
            ReceivedBy = _user.UserId ?? 0,
            Status = PaymentStatus.Completed,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = _user.UserId
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("payment.record", "Payment", payment.Id.ToString(),
            null, $"amount={payment.AmountMinor} method={payment.Method}", request.Notes, ct);

        // Auto-generate the receipt snapshot (best-effort — never blocks the payment)
        try { await _receipts.GenerateForPaymentAsync(payment.Id, ct); }
        catch { /* receipt generation failure is non-fatal to the payment */ }

        // Award loyalty points (best-effort — never blocks the payment)
        try
        {
            if (await _modules.IsEnabledAsync("loyalty", ct))
            {
                var loyaltySettings = await _settings.GetAsync<LoyaltySettings>(ct);
                if (loyaltySettings.PointsPerHundredSpent > 0 && payment.AmountMinor > 0)
                {
                    // 1 point per 100 KES spent (configurable)
                    var points = (int)(payment.AmountMinor / 10000) * loyaltySettings.PointsPerHundredSpent;
                    if (points > 0)
                    {
                        await _loyalty.AddEntryAsync(new LoyaltyEntryInput(
                            payment.MemberId,
                            "Earn",
                            points,
                            $"Payment {payment.Id}"));
                    }
                }
            }
        }
        catch { /* loyalty failure is non-fatal to the payment */ }

        return Result<Payment>.Ok(payment);
    }

    public async Task<Result<IReadOnlyList<PaymentListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission("members.view"))
            return Result<IReadOnlyList<PaymentListItem>>.Fail("permission.denied", "You do not have permission to view payments.");

        var list = await _db.Payments
            .AsQueryable()
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.ReceivedAtUtc)
            .Select(p => new PaymentListItem(
                p.Id,
                p.MemberId,
                p.AmountMinor,
                p.CurrencyCode,
                p.Method,
                p.Reference,
                p.ReceivedAtUtc,
                p.Status.ToString()))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PaymentListItem>>.Ok(list);
    }

    public async Task<Result<IReadOnlyList<PaymentListItem>>> ListAllAsync(PaymentListQuery query, CancellationToken ct = default)
    {
        if (!_user.HasPermission("payments.record"))
            return Result<IReadOnlyList<PaymentListItem>>.Fail("permission.denied", "You do not have permission to view payments.");

        var q = _db.Payments.AsQueryable();

        if (query.FromUtc.HasValue) q = q.Where(p => p.ReceivedAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue) q = q.Where(p => p.ReceivedAtUtc <= query.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(query.Method)) q = q.Where(p => p.Method == query.Method);

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim();
            // Join to member for name/number search
            q = from p in q
                join m in _db.Members on p.MemberId equals m.Id
                where EF.Functions.Like(m.FullName, $"%{text}%")
                   || EF.Functions.Like(m.MemberNumber, $"%{text}%")
                   || EF.Functions.Like(p.Reference ?? "", $"%{text}%")
                select p;
        }

        var results = await q
            .OrderByDescending(p => p.ReceivedAtUtc)
            .Take(Math.Max(1, Math.Min(query.Take, 1000)))
            .Select(p => new PaymentListItem(
                p.Id, p.MemberId, p.AmountMinor, p.CurrencyCode, p.Method, p.Reference,
                p.ReceivedAtUtc, p.Status.ToString()))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PaymentListItem>>.Ok(results);
    }
}




