namespace FitCore.Domain.Entities;

public class ProgressEntry : AuditableEntity
{
    public int MemberId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? BodyFatPercent { get; set; }
    public string? MeasurementsJson { get; set; }
    public string? PhotoPath { get; set; }
    public string? Goal { get; set; }
    public string? Notes { get; set; }
}
