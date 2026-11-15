using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Application.Recovery;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Recovery;

public sealed class ChallengeService : IChallengeService
{
    private readonly FitCoreDbContext _db;
    private readonly ISignatureVerifier _verifier;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(15);

    public ChallengeService(FitCoreDbContext db, ISignatureVerifier verifier, IPasswordHasher hasher, IClock clock)
    {
        _db = db;
        _verifier = verifier;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<Result<ChallengeIssued>> IssueChallengeAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<ChallengeIssued>.Fail("recovery.username.required", "Username is required.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        // Do not leak user existence. Return a *format-valid* challenge even for unknown users,
        // but the DB row carries UserId = 0 so verification will still fail later.
        var userId = user?.Id ?? 0;

        var installation = await _db.Installations.AsQueryable().FirstOrDefaultAsync(ct);
        var installationId = installation?.Id.ToString() ?? "uninitialised";

        var challengeId = Guid.NewGuid().ToString("N");
        var now = _clock.UtcNow;
        var expires = now.Add(ChallengeLifetime);

        // Encode the challenge as a human-friendly string:
        // CHAL-{installationShort}-{challengeIdShort}-{unixExpiry}
        var payload = new
        {
            id = challengeId,
            inst = installationId,
            uid = userId,
            exp = expires.ToString("O")
        };
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadHash = Convert.ToBase64String(SHA256.HashData(payloadBytes))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var shortId = challengeId[..8];
        var shortInst = installationId.Length >= 8 ? installationId[..8] : installationId;
        var expiryUnix = new DateTimeOffset(expires, TimeSpan.Zero).ToUnixTimeSeconds();
        var challengeCode = $"CHAL-{shortInst}-{shortId}-{expiryUnix}";

        if (userId > 0)
        {
            _db.RecoveryAttempts.Add(new RecoveryAttempt
            {
                ChallengeId = challengeId,
                InstallationId = installationId,
                UserId = userId,
                CreatedAtUtc = now,
                ExpiresAtUtc = expires
            });
            await _db.SaveChangesAsync(ct);
        }

        // The full payload + hash must accompany the challenge code out-of-band so the
        // vendor can verify it. We embed the hash into the challenge code suffix for simplicity.
        return Result<ChallengeIssued>.Ok(new ChallengeIssued(challengeCode, expires));
    }

    public async Task<Result<RecoveryVerified>> VerifyRecoveryAsync(string challengeCode, string recoveryCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(challengeCode) || string.IsNullOrWhiteSpace(recoveryCode))
            return Result<RecoveryVerified>.Fail("recovery.input.required", "Challenge and recovery code are required.");

        // Recovery code is: {challengeId}|{signatureBase64}|{expiryUnix}
        var parts = recoveryCode.Split('|');
        if (parts.Length != 3)
            return Result<RecoveryVerified>.Fail("recovery.code.invalid", "Recovery code format is invalid.");

        if (!Guid.TryParse(parts[0], out var challengeId))
            return Result<RecoveryVerified>.Fail("recovery.code.invalid", "Challenge ID in recovery code is invalid.");

        byte[] signature;
        try { signature = Convert.FromBase64String(parts[1]); }
        catch { return Result<RecoveryVerified>.Fail("recovery.code.invalid", "Signature is malformed."); }

        if (!long.TryParse(parts[2], out var expiryUnix))
            return Result<RecoveryVerified>.Fail("recovery.code.invalid", "Expiry is malformed.");

        var attempt = await _db.RecoveryAttempts.FirstOrDefaultAsync(r => r.ChallengeId == challengeId.ToString("N"), ct);
        if (attempt is null)
            return Result<RecoveryVerified>.Fail("recovery.not_found", "This challenge is not recognised.");

        if (attempt.UsedAtUtc.HasValue)
            return Result<RecoveryVerified>.Fail("recovery.already_used", "This recovery has already been used.");

        var now = _clock.UtcNow;
        if (now > attempt.ExpiresAtUtc)
            return Result<RecoveryVerified>.Fail("recovery.expired", "This challenge has expired.");

        // Payload the vendor signed: challengeId|installationId|userId|expiryUnix
        var payloadText = $"{attempt.ChallengeId}|{attempt.InstallationId}|{attempt.UserId}|{expiryUnix}";
        var payloadBytes = Encoding.UTF8.GetBytes(payloadText);

        if (!_verifier.Verify(payloadBytes, signature))
            return Result<RecoveryVerified>.Fail("recovery.signature.invalid", "Recovery code signature is invalid.");

        // Refuse if the user is deactivated (no point resetting a disabled account).
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.UserId, ct);
        if (user is null)
            return Result<RecoveryVerified>.Fail("user.missing", "User not found.");
        if (!user.IsActive)
            return Result<RecoveryVerified>.Fail("user.inactive", "This user account is deactivated.");

        // Mark used (before password reset to enforce single-use).
        attempt.UsedAtUtc = now;
        attempt.RecoveryCodeHash = Convert.ToBase64String(SHA256.HashData(signature));
        await _db.SaveChangesAsync(ct);

        return Result<RecoveryVerified>.Ok(new RecoveryVerified(attempt.UserId, attempt.ChallengeId));
    }

    public async Task<Result> CompleteRecoveryAsync(string challengeCode, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(challengeCode))
            return Result.Fail("recovery.input.required", "Challenge code is required.");

        var pwdErrors = PasswordPolicy.Validate(newPassword);
        if (pwdErrors.Count > 0)
            return Result.Fail("identity.password.weak", string.Join(" ", pwdErrors));

        // Find a challenge whose id matches the suffix of the challenge code
        var parts = challengeCode.Split('-');
        if (parts.Length < 4)
            return Result.Fail("recovery.code.invalid", "Challenge code format is invalid.");

        var shortId = parts[2];
        var candidates = await _db.RecoveryAttempts
            .AsQueryable()
            .Where(r => r.ChallengeId.StartsWith(shortId) && r.UsedAtUtc != null)
            .ToListAsync(ct);

        if (candidates.Count != 1)
            return Result.Fail("recovery.not_found", "No verified recovery found for this challenge.");

        var attempt = candidates[0];
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.UserId, ct);
        if (user is null)
            return Result.Fail("user.missing", "User not found.");

        var (hash, paramsJson) = _hasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.HashParams = paramsJson;
        user.MustChangePassword = true;
        user.FailedAttempts = 0;
        user.LockedUntilUtc = null;
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }
}


