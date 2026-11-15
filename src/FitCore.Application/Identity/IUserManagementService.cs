using FitCore.Application.Common;
using FitCore.Domain.Entities;

namespace FitCore.Application.Identity;

/// <summary>
/// User management (PRS §11). All operations audit-logged and permission-checked at service layer.
/// Enforces "at least one active Owner must always exist" invariant.
/// </summary>
public interface IUserManagementService
{
    /// <summary>Create a new non-owner user with a role.</summary>
    Task<Result<User>> CreateUserAsync(string username, string displayName, string password, int roleId, CancellationToken ct = default);

    /// <summary>Change a user's role. Blocked if it would demote the last active Owner.</summary>
    Task<Result> ChangeRoleAsync(int userId, int newRoleId, string? reason, CancellationToken ct = default);

    /// <summary>Activate or deactivate a user. Blocked if it would deactivate the last active Owner.</summary>
    Task<Result> SetActiveAsync(int userId, bool isActive, string? reason, CancellationToken ct = default);

    /// <summary>Change a user's own password (requires current password).</summary>
    Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Admin reset: change another user's password (requires password reset permission; caller checks).</summary>
    Task<Result> ResetPasswordAsync(int userId, string newPassword, string? reason, CancellationToken ct = default);

    /// <summary>List all users (for the Settings -> Users page).</summary>
    Task<Result<IReadOnlyList<UserListItem>>> ListUsersAsync(CancellationToken ct = default);

    /// <summary>List all roles (for the role assignment dropdown).</summary>
    Task<Result<IReadOnlyList<RoleListItem>>> ListRolesAsync(CancellationToken ct = default);
}

