namespace FitCore.Application.Identity;

public sealed record UserListItem(
    int Id,
    string Username,
    string DisplayName,
    int RoleId,
    string RoleName,
    bool IsActive,
    DateTime? LastLoginUtc);

public sealed record RoleListItem(int Id, string Name, bool IsOwnerRole);
