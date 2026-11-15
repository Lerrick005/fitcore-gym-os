using System;
using System.Collections.ObjectModel;
using System.Linq;
using FitCore.Application.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Formatting;
using FitCore.Application.Members;
using FitCore.Application.Memberships;
using FitCore.Application.Payments;
using FitCore.Application.Plans;
using FitCore.Application.Settings;

namespace FitCore.Desktop.ViewModels.Members;

/// <summary>
/// Single-screen new member flow: details + plan + payment + receipt.
/// Reduces 6 clicks to 2.
/// </summary>
public sealed partial class NewMemberFlowViewModel : ObservableObject
{
    private readonly IMemberService _members;
    private readonly IMembershipService _memberships;
    private readonly IPaymentService _payments;
    private readonly IMembershipPlanService _plans;
    private readonly ISettingsService _settings;
    private readonly ICurrencyFormatter _money;

    public ObservableCollection<FlowPlanCard> Plans { get; } = new();

    // Step 1: member details
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;

    // Step 2: plan
    [ObservableProperty] private FlowPlanCard? _selectedPlan;
    [ObservableProperty] private DateTime? _startDate = DateTime.UtcNow.Date;

    // Step 3: payment
    [ObservableProperty] private string _amountInput = string.Empty;
    [ObservableProperty] private string _method = "Cash";
    [ObservableProperty] private string _reference = string.Empty;

    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _totalDisplay = string.Empty;
    [ObservableProperty] private string _currencySymbol = "KSh";

    public string[] Methods { get; } = new[] { "Cash", "M-Pesa", "Card", "Bank transfer", "Other" };

    public event EventHandler<int>? MemberCreated;

    public NewMemberFlowViewModel(
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
        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        CurrencySymbol = locale.CurrencySymbol;

        var plansResult = await _plans.GetActiveAsync(ct);
        Plans.Clear();
        if (plansResult.IsSuccess)
        {
            foreach (var p in plansResult.Value!.Where(p => p.Category == "Membership" || p.Category == "Pass"))
            {
                var display = await _money.FormatAsync(p.PriceMinor, p.CurrencyCode, ct);
                Plans.Add(new FlowPlanCard(p.Id, p.Name, display, p.PriceMinor, p.CurrencyCode, p.DurationValue, p.DurationUnit));
            }
        }
    }

    partial void OnSelectedPlanChanged(FlowPlanCard? value)
    {
        if (value is null) return;
        AmountInput = (value.PriceMinor / 100m).ToString("0.00");
    }

    [RelayCommand]
    private void SelectPlan(FlowPlanCard? plan)
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

        if (string.IsNullOrWhiteSpace(FullName)) { ErrorMessage = "Full name is required."; return; }
        if (string.IsNullOrWhiteSpace(Phone)) { ErrorMessage = "Phone is required."; return; }
        if (SelectedPlan is null) { ErrorMessage = "Select a plan."; return; }
        if (!decimal.TryParse(AmountInput, out var amount) || amount < 0)
        {
            ErrorMessage = "Enter a valid payment amount.";
            return;
        }

        IsBusy = true;
        try
        {
            // Step 1: create member
            var memberResult = await _members.CreateAsync(new MemberInput(
                FullName, Phone, string.IsNullOrWhiteSpace(Email) ? null : Email,
                null, null, null, null));
            if (!memberResult.IsSuccess) { ErrorMessage = memberResult.ErrorMessage; return; }
            var member = memberResult.Value!;

            // Step 2: sell plan
            var sellResult = await _memberships.SellAsync(new SellMembershipRequest(
                member.Id, SelectedPlan.Id, StartDate?.Date, null));
            if (!sellResult.IsSuccess) { ErrorMessage = sellResult.ErrorMessage; return; }
            var membership = sellResult.Value!;

            // Step 3: record payment if amount > 0
            if (amount > 0)
            {
                var locale = await _settings.GetAsync<LocaleSettings>();
                var amountMinor = (long)(amount * (decimal)Math.Pow(10, locale.DecimalPlaces));
                var paymentResult = await _payments.RecordAsync(new RecordPaymentRequest(
                    member.Id, membership.Id, amountMinor, locale.CurrencyCode,
                    Method, string.IsNullOrWhiteSpace(Reference) ? null : Reference, null));
                if (!paymentResult.IsSuccess)
                {
                    // Member + membership exist; payment failed. Surface but don't rollback.
                    ErrorMessage = $"Member created but payment failed: {paymentResult.ErrorMessage}. You can retry from the member's page.";
                    IsBusy = false;
                    MemberCreated?.Invoke(this, member.Id);
                    return;
                }
            }

            MemberCreated?.Invoke(this, member.Id);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel()
    {
        MemberCreated?.Invoke(this, -1); // -1 = cancel
    }
}

public sealed partial class FlowPlanCard : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string PriceDisplay { get; }
    public long PriceMinor { get; }
    public string CurrencyCode { get; }
    public int DurationValue { get; }
    public string DurationUnit { get; }

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public string DurationDisplay => DurationUnit switch
    {
        "Days" => DurationValue == 1 ? "1 day" : $"{DurationValue} days",
        "Weeks" => DurationValue == 1 ? "1 week" : $"{DurationValue} weeks",
        "Months" => DurationValue == 1 ? "1 month" : $"{DurationValue} months",
        _ => $"{DurationValue} {DurationUnit}"
    };

    public FlowPlanCard(int id, string name, string priceDisplay, long priceMinor, string currencyCode, int durationValue, string durationUnit)
    {
        Id = id; Name = name; PriceDisplay = priceDisplay; PriceMinor = priceMinor;
        CurrencyCode = currencyCode; DurationValue = durationValue; DurationUnit = durationUnit;
    }
}

