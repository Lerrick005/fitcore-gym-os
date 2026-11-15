namespace FitCore.Domain.Entities;

public class WorkoutEntry : AuditableEntity
{
    public int MemberId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }
}
