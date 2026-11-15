using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Loyalty;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class RewardsSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ILoyaltyRewardService _rewards;

    public ObservableCollection<LoyaltyRewardRow> Rows { get; } = new();

    [ObservableProperty] private bool _showInactive;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessageLocal;

    public override string GroupName => "Loyalty Rewards";
    public override string GroupDescription => "Define what members can claim with their loyalty points.";

    public event EventHandler<int?>? EditRequested;

    public RewardsSettingsViewModel(ILoyaltyRewardService rewards) => _rewards = rewards;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var r = await _rewards.ListAsync(ShowInactive, ct);
        Rows.Clear();
        if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
        foreach (var x in r.Value!) Rows.Add(x);
    }

    partial void OnShowInactiveChanged(bool value) => _ = LoadAsync();

    [RelayCommand]
    private void AddNew() => EditRequested?.Invoke(this, null);

    [RelayCommand]
    private void EditReward(LoyaltyRewardRow? row) => EditRequested?.Invoke(this, row?.Id);

    protected override Task<FitCore.Application.Common.Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
