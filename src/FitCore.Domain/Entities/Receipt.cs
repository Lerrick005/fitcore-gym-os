namespace FitCore.Domain.Entities;

public class Receipt
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int PaymentId { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public int IssuedBy { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public int? VoidedByReceiptId { get; set; }
}
