namespace FitCore.Domain.Entities;

public class TrainerAssignment : AuditableEntity
{
    public int TrainerId { get; set; }
    public int MemberId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
}
