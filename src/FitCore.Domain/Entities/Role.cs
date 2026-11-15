namespace FitCore.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; }
    public bool IsOwnerRole { get; set; }
}
