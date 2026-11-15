using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Loyalty;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class RewardEditViewModel : ObservableObject
{
    private readonly ILoyaltyRewardService _rewards;
    private int? _rewardId;

    [ObservableProperty] private string _title = "Add reward";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _pointsRequired = "100";
    [ObservableProperty] private string _rewardType = "FreeDays";
    [ObservableProperty] private string _valueInput = "2";
    [ObservableProperty] private DateTime? _expiresAt;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] RewardTypes { get; } = new[] { "FreeDays", "FixedDiscount", "PercentageDiscount", "Custom" };

    public event EventHandler<bool>? CloseRequested;

    public RewardEditViewModel(ILoyaltyRewardService rewards) => _rewards = rewards;

    public async Task InitializeAsync(int? rewardId, CancellationToken ct = default)
    {
        _rewardId = rewardId;
        Title = rewardId.HasValue ? "Edit reward" : "Add reward";
        if (!rewardId.HasValue) return;

        var list = await _rewards.ListAsync(true, ct);
        if (!list.IsSuccess) { ErrorMessage = list.ErrorMessage; return; }
        var r = System.Linq.Enumerable.FirstOrDefault(list.Value!, x => x.Id == rewardId.Value);
        if (r is null) { ErrorMessage = "Reward not found."; return; }

        Name = r.Name;
        Description = r.Description ?? string.Empty;
        PointsRequired = r.PointsRequired.ToString();
        RewardType = r.RewardType;
        ValueInput = r.ValueMinor.ToString();
        ExpiresAt = r.ExpiresAtUtc;
        IsActive = r.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        if (!int.TryParse(PointsRequired, out var pts) || pts <= 0) { ErrorMessage = "Points must be a positive number."; return; }
        if (!long.TryParse(ValueInput, out var val)) { ErrorMessage = "Value must be a number."; return; }

        IsBusy = true;
        try
        {
            var input = new LoyaltyRewardInput(
                Name.Trim(),
                string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                pts, RewardType, val, ExpiresAt, IsActive);

            var r = _rewardId.HasValue
                ? await _rewards.UpdateAsync(_rewardId.Value, input)
                : await _rewards.CreateAsync(input);

            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
