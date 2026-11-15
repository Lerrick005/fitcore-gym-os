namespace FitCore.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string HashParams { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public bool MustChangePassword { get; set; }
}
