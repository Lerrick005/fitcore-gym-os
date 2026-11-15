namespace FitCore.Domain.Entities;

public class Expense : AuditableEntity
{
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Description { get; set; }
    public string? AttachmentPath { get; set; }
}
