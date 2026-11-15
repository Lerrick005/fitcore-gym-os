namespace FitCore.Application.Settings;

public sealed record RolesSettings : ISettingsGroup
{
    public static string GroupKey => "roles";

    // Role definitions themselves live in Roles/RolePermissions tables.
    // This group holds only role-related settings that are not permissions.
    public bool AllowRoleCloning { get; init; } = true;
    public bool RequireReasonForPermissionChange { get; init; } = true;
}
