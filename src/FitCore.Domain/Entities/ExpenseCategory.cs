namespace FitCore.Domain.Entities;

public class ExpenseCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
