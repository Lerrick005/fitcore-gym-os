namespace FitCore.Domain.Entities;

public class Programme : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int Capacity { get; set; } = 20;
    public int Version { get; set; }
    public string? ContentJson { get; set; }
    public bool IsActive { get; set; }
}


