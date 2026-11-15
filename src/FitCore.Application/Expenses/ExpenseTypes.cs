namespace FitCore.Application.Expenses;

public sealed record ExpenseCategoryItem(int Id, string Name, bool IsActive);
public sealed record ExpenseItem(int Id, int CategoryId, string CategoryName, int? SupplierId, long AmountMinor, string CurrencyCode, DateTime OccurredAtUtc, string? Description);
public sealed record ExpenseInput(int CategoryId, int? SupplierId, long AmountMinor, DateTime OccurredAtUtc, string? Description);
public sealed record ExpenseCategoryInput(string Name, string? Description);
