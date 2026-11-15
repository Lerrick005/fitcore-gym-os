namespace FitCore.Domain.Entities;

/// <summary>
/// Records each issued recovery challenge (PRS §11). Enforces single-use.
/// </summary>
public class RecoveryAttempt
{
    public int Id { get; set; }
    public string ChallengeId { get; set; } = string.Empty; // GUID of the challenge
    public string InstallationId { get; set; } = string.Empty; // captured for audit
    public int UserId { get; set; } // which user requested recovery
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public string? RecoveryCodeHash { get; set; } // hash of the recovery code when used (to detect reuse)
}
