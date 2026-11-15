using FitCore.Application.Common;

namespace FitCore.Application.Expenses;

public interface IExpenseService
{
    Task<Result<IReadOnlyList<ExpenseCategoryItem>>> ListCategoriesAsync(CancellationToken ct = default);
    Task<Result<ExpenseCategoryItem>> CreateCategoryAsync(ExpenseCategoryInput input, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ExpenseItem>>> ListAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
    Task<Result<ExpenseItem>> CreateAsync(ExpenseInput input, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
