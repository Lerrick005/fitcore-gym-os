using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class Member : AuditableEntity
{
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContact { get; set; }
    public string? PhotoPath { get; set; }
    public MemberStatus Status { get; set; }
    public DateTime? ConsentRecordedAtUtc { get; set; }
    public string? ConsentVersion { get; set; }
    public string? Notes { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}
