using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Identity;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed class UserRow
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string LastLoginDisplay { get; init; } = "—";
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}

public sealed partial class UsersSettingsViewModel : SettingsPageViewModelBase
{
    private readonly IUserManagementService _users;

    [ObservableProperty] private ObservableCollection<UserRow> _rows = new();
    [ObservableProperty] private string? _statusMessageLocal;

    public override string GroupName => "Users";
    public override string GroupDescription => "Staff accounts. Add receptionists, managers, trainers.";

    public event EventHandler<int?>? EditRequested;
    public event EventHandler<int>? ToggleActiveRequested;

    public UsersSettingsViewModel(IUserManagementService users) => _users = users;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var result = await _users.ListUsersAsync(ct);
        Rows.Clear();
        if (!result.IsSuccess) { StatusMessage = result.ErrorMessage; return; }
        foreach (var u in result.Value!)
        {
            Rows.Add(new UserRow
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                RoleId = u.RoleId,
                RoleName = u.RoleName,
                IsActive = u.IsActive,
                LastLoginDisplay = u.LastLoginUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—"
            });
        }
    }

    [RelayCommand]
    private void AddNew() => EditRequested?.Invoke(this, null);

    [RelayCommand]
    private void EditUser(UserRow? row)
    {
        if (row is null) return;
        EditRequested?.Invoke(this, row.Id);
    }

    [RelayCommand]
    private void ToggleActive(UserRow? row)
    {
        if (row is null) return;
        ToggleActiveRequested?.Invoke(this, row.Id);
    }

    protected override Task<FitCore.Application.Common.Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
