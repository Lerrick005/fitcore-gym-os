using FitCore.Application.Common;

namespace FitCore.Application.Identity;

public sealed record AuthenticatedUser(int UserId, string Username, string DisplayName, int RoleId);

/// <summary>Local authentication (PRS §11). Argon2id passwords, lockout with backoff.</summary>
public interface IAuthenticationService
{
    /// <summary>Attempt login. Returns user on success; failure result on any error.</summary>
    Task<Result<AuthenticatedUser>> LoginAsync(string username, string password, CancellationToken ct = default);

    /// <summary>Clear the current session.</summary>
    void Logout();
}
