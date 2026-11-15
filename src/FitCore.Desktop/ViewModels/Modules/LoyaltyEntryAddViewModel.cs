using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Loyalty;
using FitCore.Application.Members;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class LoyaltyEntryAddViewModel : ObservableObject
{
    private readonly ILoyaltyService _loyalty;
    private readonly IMemberService _members;

    public ObservableCollection<MemberListItem> Members { get; } = new();

    [ObservableProperty] private MemberListItem? _selectedMember;
    [ObservableProperty] private string _type = "Earn";
    [ObservableProperty] private string _points = "10";
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] Types { get; } = new[] { "Earn", "Redeem", "Adjustment" };

    public event EventHandler<bool>? CloseRequested;

    public LoyaltyEntryAddViewModel(ILoyaltyService loyalty, IMemberService members)
    {
        _loyalty = loyalty;
        _members = members;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var r = await _members.SearchAsync(new MemberSearchQuery { Take = 500 }, ct);
        Members.Clear();
        if (r.IsSuccess) foreach (var m in r.Value!) Members.Add(m);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedMember is null) { ErrorMessage = "Select a member."; return; }
        if (!int.TryParse(Points, out var pts) || pts <= 0) { ErrorMessage = "Points must be a positive number."; return; }

        IsBusy = true;
        try
        {
            var input = new LoyaltyEntryInput(SelectedMember.Id, Type, pts, string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim());
            var r = await _loyalty.AddEntryAsync(input);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
