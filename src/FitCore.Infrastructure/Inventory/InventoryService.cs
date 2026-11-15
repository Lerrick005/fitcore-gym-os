using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Inventory;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Inventory;

public sealed class InventoryService : IInventoryService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;

    public InventoryService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("inventory", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<InventoryItemRow>>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<InventoryItemRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("inventory.manage")) return Result<IReadOnlyList<InventoryItemRow>>.Fail("permission.denied", "No permission.");

        var q = _db.InventoryItems.AsQueryable();
        if (!includeInactive) q = q.Where(i => i.IsActive);
        var rows = await q.OrderBy(i => i.Name).Select(i => new InventoryItemRow(
            i.Id, i.Name, i.Sku, i.Category, i.StockLevel, i.LowStockThreshold, i.IsActive, i.UnitPriceMinor, i.CurrencyCode)).ToListAsync(ct);
        return Result<IReadOnlyList<InventoryItemRow>>.Ok(rows);
    }

    public async Task<Result<IReadOnlyList<InventoryItemRow>>> ListLowStockAsync(CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<InventoryItemRow>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        var rows = await _db.InventoryItems.AsQueryable()
            .Where(i => i.IsActive && i.StockLevel <= i.LowStockThreshold)
            .OrderBy(i => i.Name)
            .Select(i => new InventoryItemRow(i.Id, i.Name, i.Sku, i.Category, i.StockLevel, i.LowStockThreshold, i.IsActive, i.UnitPriceMinor, i.CurrencyCode))
            .ToListAsync(ct);
        return Result<IReadOnlyList<InventoryItemRow>>.Ok(rows);
    }

    public async Task<Result<InventoryItemRow>> CreateAsync(InventoryItemInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<InventoryItemRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("inventory.manage")) return Result<InventoryItemRow>.Fail("permission.denied", "No permission.");
        if (string.IsNullOrWhiteSpace(input.Name)) return Result<InventoryItemRow>.Fail("validation.name", "Name required.");

        var sku = string.IsNullOrWhiteSpace(input.Sku) ? await GenerateSkuAsync(ct) : input.Sku.Trim();

        var item = new InventoryItem
        {
            Name = input.Name.Trim(), Sku = sku, Category = input.Category,
            StockLevel = input.StockLevel, LowStockThreshold = input.LowStockThreshold,
            UnitPriceMinor = input.UnitPriceMinor, CurrencyCode = input.CurrencyCode,
            IsActive = true, CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("inventory.create", "InventoryItem", item.Id.ToString(), null, item.Name, null, ct);
        return Result<InventoryItemRow>.Ok(new InventoryItemRow(item.Id, item.Name, item.Sku, item.Category, item.StockLevel, item.LowStockThreshold, item.IsActive, item.UnitPriceMinor, item.CurrencyCode));
    }

    public async Task<Result<InventoryItemRow>> UpdateAsync(int id, InventoryItemInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<InventoryItemRow>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("inventory.manage")) return Result<InventoryItemRow>.Fail("permission.denied", "No permission.");

        var item = await _db.InventoryItems.AsQueryable().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return Result<InventoryItemRow>.Fail("item.not_found", "Item not found.");

        item.Name = input.Name.Trim(); item.Sku = input.Sku; item.Category = input.Category;
        item.StockLevel = input.StockLevel; item.LowStockThreshold = input.LowStockThreshold;
        item.UnitPriceMinor = input.UnitPriceMinor; item.CurrencyCode = input.CurrencyCode;
        item.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("inventory.update", "InventoryItem", item.Id.ToString(), null, item.Name, null, ct);
        return Result<InventoryItemRow>.Ok(new InventoryItemRow(item.Id, item.Name, item.Sku, item.Category, item.StockLevel, item.LowStockThreshold, item.IsActive, item.UnitPriceMinor, item.CurrencyCode));
    }

    public async Task<Result> AdjustStockAsync(StockAdjustment input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("inventory.manage")) return Result.Fail("permission.denied", "No permission.");

        var item = await _db.InventoryItems.AsQueryable().FirstOrDefaultAsync(i => i.Id == input.ItemId, ct);
        if (item is null) return Result.Fail("item.not_found", "Item not found.");

        var newLevel = item.StockLevel + input.Delta;
        if (newLevel < 0) return Result.Fail("stock.negative", "Stock cannot go negative.");

        item.StockLevel = newLevel;
        item.UpdatedAtUtc = _clock.UtcNow;
        _db.StockMovements.Add(new StockMovement
        {
            InventoryItemId = item.Id,
            Type = input.Delta >= 0 ? StockMovementType.In : StockMovementType.Out,
            Quantity = Math.Abs(input.Delta),
            OccurredAtUtc = _clock.UtcNow,
            Notes = input.Reason,
            CreatedAtUtc = _clock.UtcNow, UpdatedAtUtc = _clock.UtcNow, CreatedBy = _user.UserId
        });
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("inventory.adjust", "InventoryItem", item.Id.ToString(), null, $"delta={input.Delta} new={newLevel}", input.Reason, ct);
        return Result.Ok();
    }

    private async Task<string> GenerateSkuAsync(CancellationToken ct)
    {
        // Use sequence counter for auto-SKU: INV-0001, INV-0002, ...
        var counter = await _db.SequenceCounters.FirstOrDefaultAsync(c => c.Name == "inventory_sku", ct);
        if (counter is null)
        {
            counter = new Domain.Entities.SequenceCounter { Name = "inventory_sku", Prefix = "INV-", NextValue = 1 };
            _db.SequenceCounters.Add(counter);
            await _db.SaveChangesAsync(ct);
        }
        var sku = $"{counter.Prefix}{counter.NextValue:D4}";
        counter.NextValue += 1;
        await _db.SaveChangesAsync(ct);
        return sku;
    }
}

