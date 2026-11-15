namespace FitCore.Domain.Entities;

/// <summary>Columns present on every business table (PRS §13).</summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int? CreatedBy { get; set; }
}
