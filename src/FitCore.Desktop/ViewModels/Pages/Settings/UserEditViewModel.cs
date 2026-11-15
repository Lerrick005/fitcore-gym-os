using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Identity;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class UserEditViewModel : ObservableObject
{
    private readonly IUserManagementService _users;
    private int? _userId;

    [ObservableProperty] private string _title = "Add staff";
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private RoleOption? _selectedRole;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEditing; // true when editing, false when adding

    public ObservableCollection<RoleOption> Roles { get; } = new();

    public event EventHandler<bool>? CloseRequested;

    public UserEditViewModel(IUserManagementService users) => _users = users;

    public async Task InitializeAsync(int? userId, CancellationToken ct = default)
    {
        _userId = userId;
        IsEditing = userId.HasValue;
        Title = userId.HasValue ? "Edit staff" : "Add staff";

        var roles = await _users.ListRolesAsync(ct);
        Roles.Clear();
        if (roles.IsSuccess)
        {
            foreach (var r in roles.Value!)
                Roles.Add(new RoleOption(r.Id, r.Name));
        }

        if (!userId.HasValue) return;

        var users = await _users.ListUsersAsync(ct);
        if (users.IsSuccess)
        {
            var u = users.Value!.FirstOrDefault(x => x.Id == userId.Value);
            if (u is not null)
            {
                Username = u.Username;
                DisplayName = u.DisplayName;
                SelectedRole = Roles.FirstOrDefault(r => r.Id == u.RoleId);
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            if (_userId is null)
            {
                if (SelectedRole is null) { ErrorMessage = "Select a role."; return; }
                if (Password != ConfirmPassword) { ErrorMessage = "Passwords do not match."; return; }

                var result = await _users.CreateUserAsync(Username, DisplayName, Password, SelectedRole.Id);
                if (!result.IsSuccess) { ErrorMessage = result.ErrorMessage; return; }
            }
            else
            {
                if (SelectedRole is not null)
                {
                    var r = await _users.ChangeRoleAsync(_userId.Value, SelectedRole.Id, "settings edit");
                    if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
                }

                if (!string.IsNullOrEmpty(Password))
                {
                    if (Password != ConfirmPassword) { ErrorMessage = "Passwords do not match."; return; }
                    var r = await _users.ResetPasswordAsync(_userId.Value, Password, "settings edit");
                    if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
                }
            }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}

public sealed class RoleOption
{
    public int Id { get; }
    public string Name { get; }
    public RoleOption(int id, string name) { Id = id; Name = name; }
    public override string ToString() => Name;
}
