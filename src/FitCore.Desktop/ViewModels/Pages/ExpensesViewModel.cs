using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Expenses;
using FitCore.Application.Formatting;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed class ExpenseRow
{
    public int Id { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public string OccurredDisplay { get; init; } = string.Empty;
    public string AmountDisplay { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed partial class ExpensesViewModel : PageViewModelBase
{
    private readonly IExpenseService _expenses;
    private readonly ICurrencyFormatter _money;

    public ObservableCollection<ExpenseRow> Rows { get; } = new();
    public ObservableCollection<ExpenseCategoryItem> Categories { get; } = new();

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _totalDisplay = "0.00";

    public override string Title => "Expenses";

    public event EventHandler? AddCategoryRequested;
    public event EventHandler? AddExpenseRequested;

    public ExpensesViewModel(IExpenseService expenses, ICurrencyFormatter money)
    {
        _expenses = expenses;
        _money = money;
    }

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var cats = await _expenses.ListCategoriesAsync(ct);
            Categories.Clear();
            if (cats.IsSuccess) foreach (var c in cats.Value!) Categories.Add(c);

            var r = await _expenses.ListAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, ct);
            Rows.Clear();
            if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }

            long total = 0;
            foreach (var e in r.Value!)
            {
                var amt = await _money.FormatAsync(e.AmountMinor, e.CurrencyCode, ct);
                Rows.Add(new ExpenseRow
                {
                    Id = e.Id,
                    CategoryName = e.CategoryName,
                    OccurredAtUtc = e.OccurredAtUtc,
                    OccurredDisplay = e.OccurredAtUtc.ToLocalTime().ToString("dd/MM/yyyy"),
                    AmountDisplay = amt,
                    Description = e.Description
                });
                total += e.AmountMinor;
            }
            TotalDisplay = await _money.FormatAsync(total, "KES", ct);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddCategory() => AddCategoryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void AddExpense() => AddExpenseRequested?.Invoke(this, EventArgs.Empty);
}
