using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Members;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels;

public sealed partial class MembersViewModel : PageViewModelBase
{
    private readonly IMemberService _members;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private ObservableCollection<MemberListItem> _results = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusFilter = "Active"; // Active / Inactive / Archived / All
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private MemberListItem? _selectedMember;
    [ObservableProperty] private bool _hasResults;

    public string[] StatusFilters { get; } = new[] { "Active", "Inactive", "Archived", "All" };

    public override string Title => "Members";

    public event EventHandler<MemberListItem?>? EditRequested;
    public event EventHandler<int>? OpenDetailRequested;
    public event EventHandler? AddMemberFlowRequested;
    public event EventHandler? QuickSaleRequested;
    public event EventHandler? ImportRequested;

    public MembersViewModel(IMemberService members)
    {
        _members = members;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        await RefreshAsync(ct);
    }

    partial void OnSearchTextChanged(string value) => DebouncedRefresh();
    partial void OnStatusFilterChanged(string value) => _ = RefreshAsync();

    private void DebouncedRefresh()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (!token.IsCancellationRequested)
                    await RefreshAsync(token);
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var query = new MemberSearchQuery
            {
                Text = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                Status = StatusFilter,
                Take = 200
            };

            var result = await _members.SearchAsync(query, ct);
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Search failed.";
                Results.Clear();
                HasResults = false;
                return;
            }

            Results.Clear();
            foreach (var item in result.Value!) Results.Add(item);
            HasResults = Results.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshCommandAsync() => await RefreshAsync();

    [RelayCommand]
    private void AddNew()
    {
        AddMemberFlowRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Import()
    {
        ImportRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void QuickSale()
    {
        QuickSaleRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddNewSimple()
    {
        EditRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void OpenDetail(MemberListItem? item)
    {
        if (item is null) return;
        OpenDetailRequested?.Invoke(this, item.Id);
    }

    [RelayCommand]
    private void EditMember(MemberListItem? item)
    {
        if (item is null) return;
        EditRequested?.Invoke(this, item);
    }

    [RelayCommand]
    private async Task ArchiveMemberAsync(MemberListItem? item)
    {
        if (item is null) return;
        var result = await _members.ArchiveAsync(item.Id, null);
        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }
        StatusMessage = $"{item.FullName} archived. Switch filter to 'Archived' to restore.";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RestoreMemberAsync(MemberListItem? item)
    {
        if (item is null) return;
        var result = await _members.RestoreAsync(item.Id);
        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }
        StatusMessage = $"{item.FullName} restored.";
        await RefreshAsync();
    }
}





