using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Identity;

public sealed class AuthenticationService : IAuthenticationService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly FitCoreDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly CurrentUser _currentUser;

    public AuthenticationService(FitCoreDbContext db, IPasswordHasher hasher, IClock clock, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        if (currentUser is not CurrentUser concrete)
            throw new InvalidOperationException("IAuthenticationService requires the real CurrentUser implementation.");
        _currentUser = concrete;
    }

    public async Task<Result<AuthenticatedUser>> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<AuthenticatedUser>.Fail("auth.username.required", "Username is required.");
        if (string.IsNullOrEmpty(password))
            return Result<AuthenticatedUser>.Fail("auth.password.required", "Password is required.");

        var now = _clock.UtcNow;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        // Same generic message to prevent username enumeration
        if (user is null)
            return Result<AuthenticatedUser>.Fail("auth.invalid", "Invalid username or password.");

        if (!user.IsActive)
            return Result<AuthenticatedUser>.Fail("auth.inactive", "This account is inactive.");

        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc > now)
        {
            var remaining = (int)Math.Ceiling((user.LockedUntilUtc.Value - now).TotalMinutes);
            return Result<AuthenticatedUser>.Fail("auth.locked",
                $"Account locked. Try again in {remaining} minute(s).");
        }

        var valid = _hasher.Verify(password, user.PasswordHash, user.HashParams);

        if (!valid)
        {
            user.FailedAttempts += 1;
            if (user.FailedAttempts >= MaxFailedAttempts)
            {
                // Set lock; this attempt still reports "invalid" (standard rate-limit UX).
                // The NEXT attempt will be rejected with "auth.locked".
                user.LockedUntilUtc = now.Add(LockoutDuration);
                await _db.SaveChangesAsync(ct);
                return Result<AuthenticatedUser>.Fail("auth.invalid",
                    "Invalid username or password.");
            }
            await _db.SaveChangesAsync(ct);
            return Result<AuthenticatedUser>.Fail("auth.invalid",
                $"Invalid username or password. {MaxFailedAttempts - user.FailedAttempts} attempt(s) remaining.");
        }

        // Success
        user.FailedAttempts = 0;
        user.LockedUntilUtc = null;
        user.LastLoginUtc = now;
        await _db.SaveChangesAsync(ct);

        _currentUser.SetSession(user.Id, user.Username, user.RoleId);

        return Result<AuthenticatedUser>.Ok(
            new AuthenticatedUser(user.Id, user.Username, user.DisplayName, user.RoleId));
    }

    public void Logout() => _currentUser.Clear();
}

