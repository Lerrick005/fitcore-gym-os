using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class FollowUp : AuditableEntity
{
    public int MemberId { get; set; }
    public DateTime DueAtUtc { get; set; }
    public string? Notes { get; set; }
    public FollowUpStatus Status { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Outcome { get; set; }
}
