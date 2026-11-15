using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Identity;

public sealed class UserManagementService : IUserManagementService
{
    private readonly FitCoreDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _user;

    public UserManagementService(FitCoreDbContext db, IPasswordHasher hasher, IClock clock, IAuditLogger audit, ICurrentUser user)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _audit = audit;
        _user = user;
    }

    public async Task<Result<User>> CreateUserAsync(
        string username, string displayName, string password, int roleId, CancellationToken ct = default)
    {
        username = (username ?? string.Empty).Trim();
        displayName = (displayName ?? string.Empty).Trim();

        if (username.Length < 3 || username.Length > 50)
            return Result<User>.Fail("identity.username.invalid", "Username must be 3..50 characters.");
        if (!IsValidUsername(username))
            return Result<User>.Fail("identity.username.invalid", "Username may contain letters, digits, '.', '_', '-' only.");

        var pwdErrors = PasswordPolicy.Validate(password);
        if (pwdErrors.Count > 0)
            return Result<User>.Fail("identity.password.weak", string.Join(" ", pwdErrors));

        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            return Result<User>.Fail("identity.username.taken", $"Username '{username}' is already taken.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
            return Result<User>.Fail("identity.role.missing", "Role not found.");
        if (role.IsOwnerRole)
            return Result<User>.Fail("identity.role.owner", "Use CreateFirstOwnerAsync to create Owner accounts.");

        var (hash, hashParams) = _hasher.Hash(password);
        var user = new User
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = hash,
            HashParams = hashParams,
            RoleId = roleId,
            IsActive = true,
            FailedAttempts = 0
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("user.create", "User", user.Id.ToString(), null,
            $"username={username} roleId={roleId}", null, ct);

        return Result<User>.Ok(user);
    }

    public async Task<Result> ChangeRoleAsync(int userId, int newRoleId, string? reason, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result.Fail("user.missing", "User not found.");

        var newRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == newRoleId, ct);
        if (newRole is null) return Result.Fail("role.missing", "Role not found.");

        if (user.RoleId == newRoleId) return Result.Ok();

        var currentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, ct);
        if (currentRole?.IsOwnerRole == true && !newRole.IsOwnerRole)
        {
            var stillHasActiveOwner = await _db.Users
                .AsQueryable()
                .AnyAsync(u => u.Id != userId && u.IsActive && u.RoleId == currentRole.Id, ct);
            if (!stillHasActiveOwner)
                return Result.Fail("identity.owner.last", "Cannot demote the last active Owner.");
        }

        var old = $"roleId={user.RoleId}";
        user.RoleId = newRoleId;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("user.change_role", "User", user.Id.ToString(), old,
            $"roleId={newRoleId}", reason, ct);

        return Result.Ok();
    }

    public async Task<Result> SetActiveAsync(int userId, bool isActive, string? reason, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result.Fail("user.missing", "User not found.");

        if (user.IsActive == isActive) return Result.Ok();

        if (!isActive)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, ct);
            if (role?.IsOwnerRole == true)
            {
                var stillHasActiveOwner = await _db.Users
                    .AsQueryable()
                    .AnyAsync(u => u.Id != userId && u.IsActive && u.RoleId == role.Id, ct);
                if (!stillHasActiveOwner)
                    return Result.Fail("identity.owner.last", "Cannot deactivate the last active Owner.");
            }
        }

        user.IsActive = isActive;
        if (!isActive) user.LockedUntilUtc = null;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(isActive ? "user.activate" : "user.deactivate", "User", user.Id.ToString(),
            null, null, reason, ct);

        return Result.Ok();
    }

    public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result.Fail("user.missing", "User not found.");

        if (!_hasher.Verify(currentPassword, user.PasswordHash, user.HashParams))
            return Result.Fail("auth.current_password.invalid", "Current password is incorrect.");

        var pwdErrors = PasswordPolicy.Validate(newPassword);
        if (pwdErrors.Count > 0)
            return Result.Fail("identity.password.weak", string.Join(" ", pwdErrors));

        var (hash, hashParams) = _hasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.HashParams = hashParams;
        user.MustChangePassword = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("user.change_password", "User", user.Id.ToString(), null, null, null, ct);
        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(int userId, string newPassword, string? reason, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result.Fail("user.missing", "User not found.");

        var pwdErrors = PasswordPolicy.Validate(newPassword);
        if (pwdErrors.Count > 0)
            return Result.Fail("identity.password.weak", string.Join(" ", pwdErrors));

        var (hash, hashParams) = _hasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.HashParams = hashParams;
        user.MustChangePassword = true;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("user.reset_password", "User", user.Id.ToString(), null, null, reason, ct);
        return Result.Ok();
    }

    private static bool IsValidUsername(string username)
    {
        foreach (var c in username)
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-') return false;
        return true;
    }

    public async Task<Result<IReadOnlyList<UserListItem>>> ListUsersAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission("users.manage"))
            return Result<IReadOnlyList<UserListItem>>.Fail("permission.denied", "You do not have permission to view users.");

        var users = await (from u in _db.Users.AsQueryable()
                           join r in _db.Roles.AsQueryable() on u.RoleId equals r.Id
                           orderby r.Name, u.Username
                           select new UserListItem(u.Id, u.Username, u.DisplayName, u.RoleId, r.Name, u.IsActive, u.LastLoginUtc))
                           .ToListAsync(ct);

        return Result<IReadOnlyList<UserListItem>>.Ok(users);
    }

    public async Task<Result<IReadOnlyList<RoleListItem>>> ListRolesAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission("users.manage"))
            return Result<IReadOnlyList<RoleListItem>>.Fail("permission.denied", "You do not have permission to view roles.");

        var roles = await _db.Roles.AsQueryable()
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItem(r.Id, r.Name, r.IsOwnerRole))
            .ToListAsync(ct);

        return Result<IReadOnlyList<RoleListItem>>.Ok(roles);
    }
}

