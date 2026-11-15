namespace FitCore.Application.Abstractions;

/// <summary>Current authenticated user context (PRS §4.3). Stub until Step 6.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Username { get; }
    int? RoleId { get; }
    bool HasPermission(string permissionKey);
}
