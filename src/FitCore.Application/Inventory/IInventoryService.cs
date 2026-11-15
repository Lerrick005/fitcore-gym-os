using FitCore.Application.Common;

namespace FitCore.Application.Inventory;

public interface IInventoryService
{
    Task<Result<IReadOnlyList<InventoryItemRow>>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<Result<InventoryItemRow>> CreateAsync(InventoryItemInput input, CancellationToken ct = default);
    Task<Result<InventoryItemRow>> UpdateAsync(int id, InventoryItemInput input, CancellationToken ct = default);
    Task<Result> AdjustStockAsync(StockAdjustment input, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InventoryItemRow>>> ListLowStockAsync(CancellationToken ct = default);
}
