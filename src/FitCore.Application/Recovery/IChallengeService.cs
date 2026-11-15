using FitCore.Application.Common;

namespace FitCore.Application.Recovery;

public sealed record ChallengeIssued(string ChallengeCode, DateTime ExpiresAtUtc);
public sealed record RecoveryVerified(int UserId, string ChallengeId);

/// <summary>
/// Owner-password recovery flow (PRS §11): challenge issue + recovery code verify.
/// No hidden backdoor; the vendor must sign every recovery.
/// </summary>
public interface IChallengeService
{
    /// <summary>Issue a challenge code for the given username. Returns challenge text + expiry.</summary>
    Task<Result<ChallengeIssued>> IssueChallengeAsync(string username, CancellationToken ct = default);

    /// <summary>Verify a recovery code against a challenge code. Returns the user ID if valid.</summary>
    Task<Result<RecoveryVerified>> VerifyRecoveryAsync(string challengeCode, string recoveryCode, CancellationToken ct = default);

    /// <summary>Reset the user's password using a valid, unused recovery verification.</summary>
    Task<Result> CompleteRecoveryAsync(string challengeCode, string newPassword, CancellationToken ct = default);
}
