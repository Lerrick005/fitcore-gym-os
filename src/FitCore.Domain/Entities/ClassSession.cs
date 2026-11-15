using FitCore.Domain.Enums;

namespace FitCore.Domain.Entities;

public class ClassSession : AuditableEntity
{
    public int ProgrammeId { get; set; }
    public DateTime SessionDate { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
}
