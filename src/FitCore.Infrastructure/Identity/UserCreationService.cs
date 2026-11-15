using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Identity;

/// <summary>
/// Minimal first-Owner creation (PRS §10 step 8, §11).
/// </summary>
public sealed class UserCreationService : IUserCreationService
{
    private const string OwnerRoleName = "Owner";

    private readonly FitCoreDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public UserCreationService(FitCoreDbContext db, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<User>> CreateFirstOwnerAsync(
        string username,
        string displayName,
        string password,
        CancellationToken ct = default)
    {
        username = (username ?? string.Empty).Trim();
        displayName = (displayName ?? string.Empty).Trim();

        if (username.Length < 3 || username.Length > 50)
            return Result<User>.Fail("identity.username.invalid", "Username must be 3..50 characters.");
        if (!IsValidUsername(username))
            return Result<User>.Fail("identity.username.invalid", "Username may contain letters, digits, '.', '_', '-' only.");
        if (string.IsNullOrWhiteSpace(displayName))
            return Result<User>.Fail("identity.displayname.required", "Display name is required.");

        var policyErrors = PasswordPolicy.Validate(password);
        if (policyErrors.Count > 0)
            return Result<User>.Fail("identity.password.weak", string.Join(" ", policyErrors));

        // Reject if an Owner user already exists (PRS §11: at least one active Owner must always exist).
        var ownerRole = await _db.Roles.FirstOrDefaultAsync(r => r.IsOwnerRole, ct);
        if (ownerRole is not null)
        {
            var existingOwner = await _db.Users.AnyAsync(u => u.RoleId == ownerRole.Id && u.IsActive, ct);
            if (existingOwner)
                return Result<User>.Fail("identity.owner.exists", "An Owner account already exists.");
        }

        var now = _clock.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Ensure Owner role exists (Step 6 seeds the full default role set).
            if (ownerRole is null)
            {
                ownerRole = new Role
                {
                    Name = OwnerRoleName,
                    IsSystemDefault = true,
                    IsOwnerRole = true
                };
                _db.Roles.Add(ownerRole);
                await _db.SaveChangesAsync(ct);
            }

            if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            {
                await tx.RollbackAsync(ct);
                return Result<User>.Fail("identity.username.taken", $"Username '{username}' is already taken.");
            }

            var (hash, hashParams) = _hasher.Hash(password);
            var user = new User
            {
                Username = username,
                DisplayName = displayName,
                PasswordHash = hash,
                HashParams = hashParams,
                RoleId = ownerRole.Id,
                IsActive = true,
                FailedAttempts = 0,
                LockedUntilUtc = null,
                LastLoginUtc = null,
                MustChangePassword = false
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return Result<User>.Ok(user);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static bool IsValidUsername(string username)
    {
        foreach (var c in username)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-') return false;
        }
        return true;
    }
}
