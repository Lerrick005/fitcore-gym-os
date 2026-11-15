namespace FitCore.Domain.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
}
