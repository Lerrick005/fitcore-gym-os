namespace FitCore.Application.Inventory;

public sealed record InventoryItemRow(int Id, string Name, string? Sku, string? Category, int StockLevel, int LowStockThreshold, bool IsActive, long? UnitPriceMinor, string? CurrencyCode);
public sealed record InventoryItemInput(string Name, string? Sku, string? Category, int StockLevel, int LowStockThreshold, long? UnitPriceMinor, string? CurrencyCode);
public sealed record StockAdjustment(int ItemId, int Delta, string? Reason);
