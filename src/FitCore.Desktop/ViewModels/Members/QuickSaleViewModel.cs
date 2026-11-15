using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Formatting;
using FitCore.Application.Members;
using FitCore.Application.Memberships;
using FitCore.Application.Payments;
using FitCore.Application.Plans;
using FitCore.Application.Settings;

namespace FitCore.Desktop.ViewModels.Members;

public sealed partial class QuickSaleViewModel : ObservableObject
{
    private readonly IMemberService _members;
    private readonly IMembershipService _memberships;
    private readonly IPaymentService _payments;
    private readonly IMembershipPlanService _plans;
    private readonly ISettingsService _settings;
    private readonly ICurrencyFormatter _money;

    public ObservableCollection<QuickSalePlanCard> Plans { get; } = new();

    [ObservableProperty] private QuickSalePlanCard? _selectedPlan;
    [ObservableProperty] private string _amountInput = string.Empty;
    [ObservableProperty] private string _method = "Cash";
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _customerName = "Walk-in";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] Methods { get; } = new[] { "Cash", "M-Pesa", "Card", "Bank transfer", "Other" };

    public event EventHandler<int>? Completed; // paymentId

    public QuickSaleViewModel(
        IMemberService members,
        IMembershipService memberships,
        IPaymentService payments,
        IMembershipPlanService plans,
        ISettingsService settings,
        ICurrencyFormatter money)
    {
        _members = members;
        _memberships = memberships;
        _payments = payments;
        _plans = plans;
        _settings = settings;
        _money = money;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var result = await _plans.GetActiveAsync(ct);
        Plans.Clear();
        if (result.IsSuccess)
        {
            foreach (var p in result.Value!)
            {
                var display = await _money.FormatAsync(p.PriceMinor, p.CurrencyCode, ct);
                Plans.Add(new QuickSalePlanCard(p.Id, p.Name, display, p.PriceMinor, p.CurrencyCode));
            }
        }
    }

    partial void OnSelectedPlanChanged(QuickSalePlanCard? value)
    {
        if (value is null) return;
        AmountInput = (value.PriceMinor / 100m).ToString("0.00");
    }

    [RelayCommand]
    private void SelectPlan(QuickSalePlanCard? plan)
    {
        if (plan is null) return;
        foreach (var p in Plans) p.IsSelected = false;
        plan.IsSelected = true;
        SelectedPlan = plan;
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        ErrorMessage = null;
        if (SelectedPlan is null) { ErrorMessage = "Select a plan."; return; }
        if (!decimal.TryParse(AmountInput, out var amount) || amount <= 0)
        { ErrorMessage = "Enter a valid amount."; return; }

        IsBusy = true;
        try
        {
            // Walk-in member with auto-generated number (uses same sequence but flagged)
            var now = DateTime.UtcNow;
            var walkinName = string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in" : CustomerName.Trim() + " (walk-in)";

            var memberResult = await _members.CreateAsync(new MemberInput(
                walkinName, "WALK-IN", null, null, null, null, "Quick sale — not a formal registration"));
            if (!memberResult.IsSuccess) { ErrorMessage = memberResult.ErrorMessage; return; }
            var member = memberResult.Value!;

            var sellResult = await _memberships.SellAsync(new SellMembershipRequest(member.Id, SelectedPlan.Id, now.Date, null));
            if (!sellResult.IsSuccess) { ErrorMessage = sellResult.ErrorMessage; return; }
            var membership = sellResult.Value!;

            var locale = await _settings.GetAsync<LocaleSettings>();
            var amountMinor = (long)(amount * (decimal)Math.Pow(10, locale.DecimalPlaces));
            var payResult = await _payments.RecordAsync(new RecordPaymentRequest(
                member.Id, membership.Id, amountMinor, locale.CurrencyCode,
                Method, string.IsNullOrWhiteSpace(Reference) ? null : Reference, "Quick sale"));
            if (!payResult.IsSuccess) { ErrorMessage = payResult.ErrorMessage; return; }

            Completed?.Invoke(this, payResult.Value!.Id);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => Completed?.Invoke(this, -1);
}

public sealed partial class QuickSalePlanCard : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string PriceDisplay { get; }
    public long PriceMinor { get; }
    public string CurrencyCode { get; }
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public QuickSalePlanCard(int id, string name, string priceDisplay, long priceMinor, string currencyCode)
    {
        Id = id; Name = name; PriceDisplay = priceDisplay; PriceMinor = priceMinor; CurrencyCode = currencyCode;
    }
}
