using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Payments;

public interface IPaymentService
{
    Task<Result<Payment>> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PaymentListItem>>> ListForMemberAsync(int memberId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PaymentListItem>>> ListAllAsync(PaymentListQuery query, CancellationToken ct = default);
}

public sealed class PaymentListQuery
{
    public string? Text { get; set; }
    public string? Method { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Take { get; set; } = 200;
}
