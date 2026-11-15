using System;
using System.Threading;
using FitCore.Application.Abstractions;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Infrastructure.Identity;

/// <summary>
/// Real current-user context (PRS §11). Holds an in-memory session.
/// Singleton: one session per app lifetime. Uses IServiceScopeFactory for the DbContext
/// so scoped DbContext instances can be used without forcing the current-user to be scoped.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private int? _userId;
    private string? _username;
    private int? _roleId;

    public bool IsAuthenticated => _userId.HasValue;
    public int? UserId => _userId;
    public string? Username => _username;
    public int? RoleId => _roleId;

    private readonly IServiceScopeFactory _scopeFactory;

    public CurrentUser(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void SetSession(int userId, string username, int roleId)
    {
        _userId = userId;
        _username = username;
        _roleId = roleId;
    }

    public void Clear()
    {
        _userId = null;
        _username = null;
        _roleId = null;
    }

    public bool HasPermission(string permissionKey)
    {
        if (!_roleId.HasValue) return false;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitCoreDbContext>();
        return db.RolePermissions
            .AsNoTracking()
            .Any(rp => rp.RoleId == _roleId.Value && rp.PermissionKey == permissionKey);
    }
}
