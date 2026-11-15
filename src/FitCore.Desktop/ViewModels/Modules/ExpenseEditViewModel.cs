using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Expenses;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class ExpenseEditViewModel : ObservableObject
{
    private readonly IExpenseService _expenses;

    public ObservableCollection<ExpenseCategoryItem> Categories { get; } = new();
    public ObservableCollection<ExpenseCategoryItem> FilteredCategories { get; } = new();

    [ObservableProperty] private string _title = "Add expense";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _amountInput = string.Empty;
    [ObservableProperty] private DateTime? _occurredAt = DateTime.UtcNow.Date;
    [ObservableProperty] private ExpenseCategoryItem? _selectedCategory;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public ExpenseEditViewModel(IExpenseService expenses) => _expenses = expenses;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var cats = await _expenses.ListCategoriesAsync(ct);
        Categories.Clear();
        if (cats.IsSuccess) foreach (var c in cats.Value!) { Categories.Add(c); FilteredCategories.Add(c); }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedCategory is null) { ErrorMessage = "Select or create a category."; return; }
        if (!decimal.TryParse(AmountInput, out var amount) || amount <= 0) { ErrorMessage = "Enter a valid amount."; return; }

        IsBusy = true;
        try
        {
            var input = new ExpenseInput(
                SelectedCategory.Id, null,
                (long)(amount * 100m),
                OccurredAt ?? DateTime.UtcNow,
                string.IsNullOrWhiteSpace(Description) ? null : Description);
            var r = await _expenses.CreateAsync(input);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryName)) { ErrorMessage = "Category name is required."; return; }
        var r = await _expenses.CreateCategoryAsync(new ExpenseCategoryInput(CategoryName.Trim(), null));
        if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
        Categories.Add(r.Value!);
        FilteredCategories.Add(r.Value!);
        SelectedCategory = r.Value!;
        CategoryName = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
