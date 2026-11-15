using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class StockMovement : AuditableEntity
{
    public int InventoryItemId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
