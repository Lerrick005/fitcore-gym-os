namespace FitCore.Domain.Entities;

public class HolidayOverride : AuditableEntity
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
