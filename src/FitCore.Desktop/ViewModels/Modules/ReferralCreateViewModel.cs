using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Loyalty;
using FitCore.Application.Members;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class ReferralCreateViewModel : ObservableObject
{
    private readonly ILoyaltyService _loyalty;
    private readonly IMemberService _members;

    public ObservableCollection<MemberListItem> Members { get; } = new();

    [ObservableProperty] private MemberListItem? _selectedMember;
    [ObservableProperty] private string _referralCode = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public ReferralCreateViewModel(ILoyaltyService loyalty, IMemberService members)
    {
        _loyalty = loyalty;
        _members = members;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var r = await _members.SearchAsync(new MemberSearchQuery { Take = 500 }, ct);
        Members.Clear();
        if (r.IsSuccess) foreach (var m in r.Value!) Members.Add(m);

        // Auto-generate a referral code
        ReferralCode = $"REF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedMember is null) { ErrorMessage = "Select a member."; return; }
        if (string.IsNullOrWhiteSpace(ReferralCode)) { ErrorMessage = "Referral code is required."; return; }

        IsBusy = true;
        try
        {
            var r = await _loyalty.CreateReferralAsync(new ReferralInput(SelectedMember.Id, ReferralCode.Trim()));
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
