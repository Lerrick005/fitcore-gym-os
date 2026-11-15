using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Expenses;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Expenses;

public sealed class ExpenseService : IExpenseService
{
    private readonly FitCoreDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IModuleAccessGuard _modules;

    public ExpenseService(FitCoreDbContext db, ICurrentUser user, IClock clock, IAuditLogger audit, IModuleAccessGuard modules)
    {
        _db = db; _user = user; _clock = clock; _audit = audit; _modules = modules;
    }

    private async Task<Result?> EnsureModuleAsync(CancellationToken ct)
    {
        var r = await _modules.RequireAsync("expenses", ct);
        return r.IsSuccess ? null : r;
    }

    public async Task<Result<IReadOnlyList<ExpenseCategoryItem>>> ListCategoriesAsync(CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ExpenseCategoryItem>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("expenses.record")) return Result<IReadOnlyList<ExpenseCategoryItem>>.Fail("permission.denied", "No permission.");

        var list = await _db.ExpenseCategories.AsQueryable()
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryItem(c.Id, c.Name, c.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyList<ExpenseCategoryItem>>.Ok(list);
    }

    public async Task<Result<ExpenseCategoryItem>> CreateCategoryAsync(ExpenseCategoryInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<ExpenseCategoryItem>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("expenses.record")) return Result<ExpenseCategoryItem>.Fail("permission.denied", "No permission.");
        if (string.IsNullOrWhiteSpace(input.Name)) return Result<ExpenseCategoryItem>.Fail("validation.name", "Name required.");

        var cat = new ExpenseCategory
        {
            Name = input.Name.Trim(),
            Description = input.Description,
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow,
            CreatedBy = _user.UserId
        };
        _db.ExpenseCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("expense.category.create", "ExpenseCategory", cat.Id.ToString(), null, cat.Name, null, ct);
        return Result<ExpenseCategoryItem>.Ok(new ExpenseCategoryItem(cat.Id, cat.Name, cat.IsActive));
    }

    public async Task<Result<IReadOnlyList<ExpenseItem>>> ListAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<IReadOnlyList<ExpenseItem>>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("expenses.record")) return Result<IReadOnlyList<ExpenseItem>>.Fail("permission.denied", "No permission.");

        var q = _db.Expenses.AsQueryable();
        if (fromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.OccurredAtUtc <= toUtc.Value);

        var rows = await (from e in q
                          join c in _db.ExpenseCategories.AsQueryable() on e.CategoryId equals c.Id
                          orderby e.OccurredAtUtc descending
                          select new ExpenseItem(e.Id, e.CategoryId, c.Name, e.SupplierId, e.AmountMinor, e.CurrencyCode, e.OccurredAtUtc, e.Description))
                          .Take(500).ToListAsync(ct);
        return Result<IReadOnlyList<ExpenseItem>>.Ok(rows);
    }

    public async Task<Result<ExpenseItem>> CreateAsync(ExpenseInput input, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return Result<ExpenseItem>.Fail(m.ErrorCode!, m.ErrorMessage!);
        if (!_user.HasPermission("expenses.record")) return Result<ExpenseItem>.Fail("permission.denied", "No permission.");
        if (input.AmountMinor <= 0) return Result<ExpenseItem>.Fail("validation.amount", "Amount must be positive.");

        var category = await _db.ExpenseCategories.AsQueryable().FirstOrDefaultAsync(c => c.Id == input.CategoryId, ct);
        if (category is null) return Result<ExpenseItem>.Fail("category.not_found", "Category not found.");

        var inst = await _db.Installations.AsQueryable().FirstOrDefaultAsync(ct);
        var exp = new Expense
        {
            CategoryId = input.CategoryId,
            SupplierId = input.SupplierId,
            AmountMinor = input.AmountMinor,
            CurrencyCode = "KES",
            OccurredAtUtc = input.OccurredAtUtc,
            Description = input.Description,
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow,
            CreatedBy = _user.UserId
        };
        _db.Expenses.Add(exp);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("expense.create", "Expense", exp.Id.ToString(), null, $"{category.Name}: {input.AmountMinor / 100m:N2}", null, ct);
        return Result<ExpenseItem>.Ok(new ExpenseItem(exp.Id, exp.CategoryId, category.Name, exp.SupplierId, exp.AmountMinor, exp.CurrencyCode, exp.OccurredAtUtc, exp.Description));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var m = await EnsureModuleAsync(ct); if (m is not null) return m;
        if (!_user.HasPermission("expenses.record")) return Result.Fail("permission.denied", "No permission.");

        var e = await _db.Expenses.AsQueryable().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Fail("expense.not_found", "Expense not found.");
        _db.Expenses.Remove(e);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("expense.delete", "Expense", id.ToString(), null, null, null, ct);
        return Result.Ok();
    }
}
