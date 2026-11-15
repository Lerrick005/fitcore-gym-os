using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Members;
using FitCore.Application.Loyalty;
using FitCore.Application.Memberships;
using FitCore.Domain.Entities;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Application.Formatting;

namespace FitCore.Desktop.ViewModels.Members;

public sealed partial class MemberDetailViewModel : PageViewModelBase
{
    private readonly IMemberService _members;
    private readonly IMembershipService _memberships;
    private readonly ICurrencyFormatter _money;
    private readonly IDateTimeFormatter _dates;
    private readonly ILoyaltyService _loyalty;
    private readonly ILoyaltyRewardService _loyaltyRewards;
    private readonly IModuleAccessGuard _modules;
    private int _memberId;

    [ObservableProperty] private int _id;
    [ObservableProperty] private string _memberNumber = string.Empty;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _status = "Active";
    [ObservableProperty] private string _notes = string.Empty;

    // Active membership card
    [ObservableProperty] private bool _hasActiveMembership;
    [ObservableProperty] private bool _isExpired;
    [ObservableProperty] private string _activePlanName = string.Empty;
    [ObservableProperty] private string _activePriceDisplay = string.Empty;
    [ObservableProperty] private string _activeStartDateDisplay = string.Empty;
    [ObservableProperty] private string _activeEndDateDisplay = string.Empty;
    [ObservableProperty] private string _activeStatus = string.Empty;
    [ObservableProperty] private int _activeDaysRemaining;
    [ObservableProperty] private int _activeDaysTotal;
    [ObservableProperty] private double _activeProgressPercent;
    [ObservableProperty] private bool _canFreeze;
    [ObservableProperty] private bool _canSellPlan = true;
    [ObservableProperty] private int _activeMembershipId;

    // History
    public ObservableCollection<MembershipListItem> MembershipHistory { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public override string Title => "Member";

    [ObservableProperty] private int _loyaltyPointsBalance;
    [ObservableProperty] private bool _loyaltyEnabled;
    [ObservableProperty] private string _loyaltyValueDisplay = string.Empty;
    public ObservableCollection<RedeemableReward> RedeemableRewards { get; } = new();

    public event EventHandler<int>? RedeemRewardRequested;

    public event EventHandler? BackRequested;
    public event EventHandler? SellPlanRequested;
    public event EventHandler? RenewRequested;
    public event EventHandler? RecordPaymentRequested;
    public event EventHandler? CheckInRequested;

    public MemberDetailViewModel(
        IMemberService members,
        IMembershipService memberships,
        ICurrencyFormatter money,
        IDateTimeFormatter dates,
        ILoyaltyService loyalty,
        ILoyaltyRewardService loyaltyRewards,
        IModuleAccessGuard modules)
    {
        _members = members;
        _memberships = memberships;
        _money = money;
        _dates = dates;
        _loyalty = loyalty;
        _loyaltyRewards = loyaltyRewards;
        _modules = modules;
    }

    public async Task InitializeAsync(int memberId, CancellationToken ct = default)
    {
        _memberId = memberId;
        await LoadAsync(ct);
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var memberResult = await _members.GetAsync(_memberId, ct);
            if (!memberResult.IsSuccess)
            {
                StatusMessage = memberResult.ErrorMessage;
                return;
            }
            var m = memberResult.Value!;
            Id = m.Id;
            MemberNumber = m.MemberNumber;
            FullName = m.FullName;
            Phone = m.Phone;
            Email = m.Email ?? string.Empty;
            Status = m.Status.ToString();
            Notes = m.Notes ?? string.Empty;

            await LoadActiveMembershipAsync(ct);
            await LoadHistoryAsync(ct);
            await LoadLoyaltyAsync(ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadActiveMembershipAsync(CancellationToken ct)
    {
        var active = await _memberships.GetActiveForMemberAsync(_memberId, ct);
        if (!active.IsSuccess)
        {
            HasActiveMembership = false;
            CanSellPlan = true;
            CanFreeze = false;
            IsExpired = MembershipHistory.Count > 0;
            return;
        }

        var m = active.Value!;
        HasActiveMembership = true;
        CanSellPlan = false;
        ActiveMembershipId = m.Id;
        ActivePlanName = m.PlanNameSnapshot;
        ActivePriceDisplay = await _money.FormatAsync(m.PriceMinorSnapshot, m.CurrencyCodeSnapshot, ct);
        ActiveStartDateDisplay = await _dates.FormatDateAsync(m.StartDate, ct);
        ActiveEndDateDisplay = await _dates.FormatDateAsync(m.EndDate, ct);
        ActiveStatus = m.Status.ToString();

        var today = DateTime.UtcNow.Date;
        var totalDays = (int)Math.Max(1, (m.EndDate.Date - m.StartDate.Date).TotalDays);
        var daysRemaining = (int)Math.Max(0, (m.EndDate.Date - today).TotalDays);
        ActiveDaysRemaining = daysRemaining;
        ActiveDaysTotal = totalDays;
        ActiveProgressPercent = Math.Min(100, Math.Max(0, 100.0 * (totalDays - daysRemaining) / totalDays));

        CanFreeze = m.Status == FitCore.Domain.Enums.MembershipStatus.Active;
    }

    private async Task LoadHistoryAsync(CancellationToken ct)
    {
        var list = await _memberships.ListForMemberAsync(_memberId, ct);
        MembershipHistory.Clear();
        if (list.IsSuccess)
            foreach (var item in list.Value!) MembershipHistory.Add(item);
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void SellPlan() => SellPlanRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Renew() => RenewRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void RecordPayment() => RecordPaymentRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CheckIn() => CheckInRequested?.Invoke(this, EventArgs.Empty);

    private async Task LoadLoyaltyAsync(CancellationToken ct)
    {
        LoyaltyEnabled = await _modules.IsEnabledAsync("loyalty", ct);
        if (!LoyaltyEnabled) return;

        var bal = await _loyalty.GetBalanceAsync(Id, ct);
        if (!bal.IsSuccess) return;
        LoyaltyPointsBalance = bal.Value;

        var val = await _loyalty.GetBalanceValueMinorAsync(Id, ct);
        if (val.IsSuccess && val.Value > 0)
            LoyaltyValueDisplay = await _money.FormatAsync(val.Value, "KES", ct);
        else
            LoyaltyValueDisplay = string.Empty;

        var rewards = await _loyaltyRewards.ListRedeemableForMemberAsync(Id, ct);
        RedeemableRewards.Clear();
        if (rewards.IsSuccess)
            foreach (var r in rewards.Value!) RedeemableRewards.Add(r);
    }

    [RelayCommand]
    private void RedeemReward(RedeemableReward? reward)
    {
        if (reward is null || !reward.CanAfford) return;
        RedeemRewardRequested?.Invoke(this, reward.RewardId);
    }
}


