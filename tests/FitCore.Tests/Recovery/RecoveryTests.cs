using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FitCore.Application.Recovery;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Cryptography;
using FitCore.Infrastructure.Identity;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Recovery;
using FitCore.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Recovery;

public class RecoveryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;
    private readonly Argon2PasswordHasher _hasher = new();
    private readonly string _privateKeyPath = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "tools", "FitCore.VendorTool", "Keys", "vendor-private.pem");

    public RecoveryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-rec-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<FitCoreDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        using var db = new FitCoreDbContext(_options);
        db.Database.Migrate();

        // Ensure the public key exists at AppContext.BaseDirectory\Resources
        // (copied from Desktop csproj's PreserveNewest).
        var publicKeyCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Resources", "vendor-public-key.txt"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "FitCore.Desktop", "Resources", "vendor-public-key.txt"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "tools", "FitCore.VendorTool", "Keys", "vendor-public.pem"),
        };
        var resourcesDir = Path.Combine(AppContext.BaseDirectory, "Resources");
        Directory.CreateDirectory(resourcesDir);
        var dest = Path.Combine(resourcesDir, "vendor-public-key.txt");
        if (!File.Exists(dest))
        {
            foreach (var src in publicKeyCandidates)
            {
                if (File.Exists(src))
                {
                    File.Copy(src, dest, overwrite: true);
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private FitCoreDbContext NewDb() => new(_options);

    private ChallengeService NewService(FitCoreDbContext db)
    {
        var verifier = new RsaSignatureVerifier();
        return new ChallengeService(db, verifier, _hasher, new SystemClock());
    }

    private async Task<(int userId, string username)> SeedUserAsync(FitCoreDbContext db)
    {
        var role = new Role { Name = "Owner", IsOwnerRole = true, IsSystemDefault = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var (hash, hp) = _hasher.Hash("Str0ng!Passw0rd");
        var user = new User
        {
            Username = "owner1",
            DisplayName = "Owner One",
            PasswordHash = hash,
            HashParams = hp,
            RoleId = role.Id,
            IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();
        return (user.Id, user.Username);
    }

    private async Task EnsureInstallationAsync(FitCoreDbContext db)
    {
        db.Installations.Add(new Installation
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            SchemaVersion = 1,
            AppVersionAtCreation = "1.0.0",
            SetupCompleted = true,
            SetupStep = 10
        });
        await db.SaveChangesAsync();
    }

    private string SignRecoveryCode(string challengeId, string installationId, int userId, long expiryUnix)
    {
        var privateKeyBase64 = File.ReadAllText(_privateKeyPath).Trim();
        var privateBytes = Convert.FromBase64String(privateKeyBase64);
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateBytes, out _);

        var payloadText = $"{challengeId}|{installationId}|{userId}|{expiryUnix}";
        var payloadBytes = Encoding.UTF8.GetBytes(payloadText);
        var sig = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return $"{challengeId}|{Convert.ToBase64String(sig)}|{expiryUnix}";
    }

    [Fact]
    public async Task IssueChallenge_ReturnsChallengeCode()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        await SeedUserAsync(db);
        var svc = NewService(db);

        var result = await svc.IssueChallengeAsync("owner1");
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ChallengeCode.ShouldStartWith("CHAL-");
        result.Value.ExpiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(10));
    }

    [Fact]
    public async Task VerifyRecovery_WithValidSignature_Succeeds()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        var (userId, username) = await SeedUserAsync(db);
        var svc = NewService(db);

        // Issue challenge
        var issue = await svc.IssueChallengeAsync(username);
        issue.IsSuccess.ShouldBeTrue();

        // Find the attempt in DB to get its fields for signing
        var attempt = await db.RecoveryAttempts.AsQueryable().FirstAsync();
        var expiryUnix = new DateTimeOffset(attempt.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        var recoveryCode = SignRecoveryCode(attempt.ChallengeId, attempt.InstallationId, attempt.UserId, expiryUnix);

        var verify = await svc.VerifyRecoveryAsync(issue.Value!.ChallengeCode, recoveryCode);
        verify.IsSuccess.ShouldBeTrue();
        verify.Value!.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task VerifyRecovery_WithTamperedSignature_Fails()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        var (_, username) = await SeedUserAsync(db);
        var svc = NewService(db);

        var issue = await svc.IssueChallengeAsync(username);
        var attempt = await db.RecoveryAttempts.AsQueryable().FirstAsync();
        var expiryUnix = new DateTimeOffset(attempt.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        var validCode = SignRecoveryCode(attempt.ChallengeId, attempt.InstallationId, attempt.UserId, expiryUnix);

        // Tamper: flip a byte in the signature
        var parts = validCode.Split('|');
        var sigBytes = Convert.FromBase64String(parts[1]);
        sigBytes[10] ^= 0xFF;
        var tampered = $"{parts[0]}|{Convert.ToBase64String(sigBytes)}|{parts[2]}";

        var verify = await svc.VerifyRecoveryAsync(issue.Value!.ChallengeCode, tampered);
        verify.IsSuccess.ShouldBeFalse();
        verify.ErrorCode.ShouldBe("recovery.signature.invalid");
    }

    [Fact]
    public async Task VerifyRecovery_AfterUse_FailsSingleUse()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        var (_, username) = await SeedUserAsync(db);
        var svc = NewService(db);

        var issue = await svc.IssueChallengeAsync(username);
        var attempt = await db.RecoveryAttempts.AsQueryable().FirstAsync();
        var expiryUnix = new DateTimeOffset(attempt.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        var code = SignRecoveryCode(attempt.ChallengeId, attempt.InstallationId, attempt.UserId, expiryUnix);

        // First verify: success
        (await svc.VerifyRecoveryAsync(issue.Value!.ChallengeCode, code)).IsSuccess.ShouldBeTrue();

        // Second verify: rejected
        var second = await svc.VerifyRecoveryAsync(issue.Value!.ChallengeCode, code);
        second.IsSuccess.ShouldBeFalse();
        second.ErrorCode.ShouldBe("recovery.already_used");
    }

    [Fact]
    public async Task CompleteRecovery_ChangesPassword()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        var (userId, username) = await SeedUserAsync(db);
        var svc = NewService(db);

        var issue = await svc.IssueChallengeAsync(username);
        var attempt = await db.RecoveryAttempts.AsQueryable().FirstAsync();
        var expiryUnix = new DateTimeOffset(attempt.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        var code = SignRecoveryCode(attempt.ChallengeId, attempt.InstallationId, attempt.UserId, expiryUnix);

        (await svc.VerifyRecoveryAsync(issue.Value!.ChallengeCode, code)).IsSuccess.ShouldBeTrue();
        var complete = await svc.CompleteRecoveryAsync(issue.Value!.ChallengeCode, "N3w!Passw0rdX");
        complete.IsSuccess.ShouldBeTrue();

        // Verify new password works
        var user = await db.Users.AsQueryable().FirstAsync(u => u.Id == userId);
        _hasher.Verify("N3w!Passw0rdX", user.PasswordHash, user.HashParams).ShouldBeTrue();
        user.MustChangePassword.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyRecovery_DeactivatedUser_Rejected()
    {
        using var db = NewDb();
        await EnsureInstallationAsync(db);
        var (userId, username) = await SeedUserAsync(db);

        // Deactivate the user after challenge issue
        var issue = await NewService(db).IssueChallengeAsync(username);
        var user = await db.Users.AsQueryable().FirstAsync(u => u.Id == userId);
        user.IsActive = false;
        await db.SaveChangesAsync();

        var attempt = await db.RecoveryAttempts.AsQueryable().FirstAsync();
        var expiryUnix = new DateTimeOffset(attempt.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        var code = SignRecoveryCode(attempt.ChallengeId, attempt.InstallationId, attempt.UserId, expiryUnix);

        var verify = await NewService(db).VerifyRecoveryAsync(issue.Value!.ChallengeCode, code);
        verify.IsSuccess.ShouldBeFalse();
        verify.ErrorCode.ShouldBe("user.inactive");
    }
}

