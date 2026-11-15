using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Retention;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class RetentionViewModel : PageViewModelBase
{
    private readonly IRetentionService _retention;

    public ObservableCollection<AtRiskMember> AtRisk { get; } = new();
    public ObservableCollection<FollowUpItem> FollowUps { get; } = new();
    public ObservableCollection<NewMemberRow> NewMembers { get; } = new();

    [ObservableProperty] private int _inactivityDays = 14;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    public override string Title => "Retention";

    public RetentionViewModel(IRetentionService retention) => _retention = retention;


    public event EventHandler<AtRiskMember>? CreateFollowUpRequested;
    public event EventHandler<FollowUpItem>? CompleteFollowUpRequested;
    public override async Task LoadAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var r1 = await _retention.GetAtRiskAsync(InactivityDays, ct);
            AtRisk.Clear();
            if (r1.IsSuccess) foreach (var m in r1.Value!) AtRisk.Add(m);

            var r2 = await _retention.ListFollowUpsAsync(ct);
            FollowUps.Clear();
            if (r2.IsSuccess) foreach (var f in r2.Value!) FollowUps.Add(f);

            var r3 = await _retention.ListNewMembersAsync(90, ct);
            NewMembers.Clear();
            if (r3.IsSuccess) foreach (var x in r3.Value!) NewMembers.Add(x);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CreateFollowUp(AtRiskMember? row) { if (row is not null) CreateFollowUpRequested?.Invoke(this, row); }

    [RelayCommand]
    private void CompleteFollowUp(FollowUpItem? row) { if (row is not null) CompleteFollowUpRequested?.Invoke(this, row); }
}
