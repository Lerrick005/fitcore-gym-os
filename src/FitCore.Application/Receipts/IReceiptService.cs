using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Receipts;

/// <summary>Receipt generation with immutable snapshot (PRS §16).</summary>
public interface IReceiptService
{
    /// <summary>Generate a receipt for a completed payment. Returns the receipt with snapshot.</summary>
    Task<Result<Receipt>> GenerateForPaymentAsync(int paymentId, CancellationToken ct = default);

    /// <summary>Fetch an existing receipt (for reprint).</summary>
    Task<Result<Receipt>> GetAsync(int receiptId, CancellationToken ct = default);

    /// <summary>Fetch a receipt by its payment.</summary>
    Task<Result<Receipt>> GetForPaymentAsync(int paymentId, CancellationToken ct = default);
}
