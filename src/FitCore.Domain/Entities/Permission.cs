namespace FitCore.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
