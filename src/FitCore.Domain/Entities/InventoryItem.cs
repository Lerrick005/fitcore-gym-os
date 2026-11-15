namespace FitCore.Domain.Entities;

public class InventoryItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public int StockLevel { get; set; }
    public int LowStockThreshold { get; set; }
    public long? UnitPriceMinor { get; set; }
    public string? CurrencyCode { get; set; }
    public int? SupplierId { get; set; }
    public bool IsActive { get; set; }
}
