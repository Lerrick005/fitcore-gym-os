using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Application.Abstractions;
using FitCore.Application.Setup.Dtos;
using FitCore.Application.Setup.Validators;

namespace FitCore.Desktop.ViewModels.Wizard;

public sealed partial class RolesAndOwnerViewModel : WizardStepViewModelBase
{
    private readonly IUserCreationService _users;
    private readonly IAuthenticationService _auth;
    private bool _ownerCreated;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    public override string Title => "Owner account";
    public override string Description => "Create the first Owner. You can add more staff later.";

    public RolesAndOwnerViewModel(IUserCreationService users, IAuthenticationService auth) { _users = users; _auth = auth; }

    protected override Task LoadAsyncCore(CancellationToken ct)
    {
        // No persisted settings for this step; the User row itself is the state.
        return Task.CompletedTask;
    }

    protected override async Task<string?> ValidateAsyncCore(CancellationToken ct)
    {
        var dto = new OwnerCreationDto
        {
            Username = Username,
            DisplayName = DisplayName,
            Password = Password,
            ConfirmPassword = ConfirmPassword
        };
        var r = await new RolesAndOwnerValidator().ValidateAsync(dto, ct);
        return r.IsValid ? null : string.Join(" ", r.Errors.ConvertAll(e => e.ErrorMessage));
    }

    protected override async Task<Result> SaveAsyncCore(CancellationToken ct)
    {
        // Idempotent: if the Owner already exists (wizard re-entered), skip and succeed.
        if (_ownerCreated) return Result.Ok();

        var result = await _users.CreateFirstOwnerAsync(Username, DisplayName, Password, ct);
        if (!result.IsSuccess)
            return Result.Fail(result.ErrorCode ?? "identity.create.failed", result.ErrorMessage ?? "Owner creation failed.");

        _ownerCreated = true;

        // Auto-login as the newly created owner.
        var login = await _auth.LoginAsync(Username, Password, ct);
        if (!login.IsSuccess)
            return Result.Fail(login.ErrorCode ?? "auth.auto.failed", "Owner created but auto-login failed.");

        return Result.Ok();
    }
}

