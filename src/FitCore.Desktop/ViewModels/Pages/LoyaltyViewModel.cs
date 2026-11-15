using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Loyalty;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class LoyaltyViewModel : PageViewModelBase
{
    private readonly ILoyaltyService _loyalty;
    private readonly ILoyaltyRewardService _rewards;

    public ObservableCollection<ReferralRow> Referrals { get; } = new();
    public ObservableCollection<LoyaltyEntryRow> Entries { get; } = new();
    public ObservableCollection<LoyaltyRewardRow> Rewards { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public override string Title => "Loyalty & Referrals";

    public event EventHandler? CreateReferralRequested;
    public event EventHandler? AddEntryRequested;
    public event EventHandler? AddRewardRequested;
    public event EventHandler<int?>? EditRewardRequested;

    public LoyaltyViewModel(ILoyaltyService loyalty, ILoyaltyRewardService rewards)
    {
        _loyalty = loyalty;
        _rewards = rewards;
    }

    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var r1 = await _loyalty.ListReferralsAsync(ct);
            Referrals.Clear();
            if (r1.IsSuccess) foreach (var x in r1.Value!) Referrals.Add(x);

            var r2 = await _loyalty.ListEntriesAsync(null, ct);
            Entries.Clear();
            if (r2.IsSuccess) foreach (var x in r2.Value!) Entries.Add(x);

            var r3 = await _rewards.ListAsync(false, ct);
            Rewards.Clear();
            if (r3.IsSuccess) foreach (var x in r3.Value!) Rewards.Add(x);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CreateReferral() => CreateReferralRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void AddEntry() => AddEntryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void AddReward() => AddRewardRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void EditReward(LoyaltyRewardRow? row) => EditRewardRequested?.Invoke(this, row?.Id);
}
