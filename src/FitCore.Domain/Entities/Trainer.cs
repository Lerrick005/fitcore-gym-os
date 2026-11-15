namespace FitCore.Domain.Entities;

public class Trainer : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialities { get; set; }
    public string? AvailabilityJson { get; set; }
    public decimal? CommissionRate { get; set; }
    public bool IsActive { get; set; }
}
