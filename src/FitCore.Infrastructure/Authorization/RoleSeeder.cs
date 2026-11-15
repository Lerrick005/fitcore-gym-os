using FitCore.Application.Authorization;
using FitCore.Application.Common;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Authorization;

public sealed class RoleSeeder : IRoleSeeder
{
    private readonly FitCoreDbContext _db;

    public RoleSeeder(FitCoreDbContext db)
    {
        _db = db;
    }

    public async Task<Result> SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // 1. Permissions: add missing ones (catalogue only grows; never delete).
            var existingPerms = await _db.Permissions.AsQueryable().Select(p => p.Key).ToListAsync(ct);
            var existingPermSet = new HashSet<string>(existingPerms, StringComparer.Ordinal);

            foreach (var def in PermissionCatalogue.All)
            {
                if (existingPermSet.Contains(def.Key)) continue;
                _db.Permissions.Add(new Permission
                {
                    Key = def.Key,
                    Category = def.Category,
                    Description = def.Description
                });
            }
            await _db.SaveChangesAsync(ct);

            // 2. Default roles: create if missing by name (respects user edits to existing roles).
            var existingRoles = await _db.Roles.AsQueryable().ToListAsync(ct);
            var existingRoleNames = new HashSet<string>(
                existingRoles.Select(r => r.Name), StringComparer.Ordinal);

            foreach (var roleDef in DefaultRoleCatalogue.All)
            {
                if (existingRoleNames.Contains(roleDef.Name))
                {
                    // Role already exists — don't touch its permissions (owner may have edited).
                    continue;
                }

                var role = new Role
                {
                    Name = roleDef.Name,
                    IsSystemDefault = true,
                    IsOwnerRole = roleDef.IsOwner
                };
                _db.Roles.Add(role);
                await _db.SaveChangesAsync(ct);

                foreach (var permKey in roleDef.Permissions)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionKey = permKey
                    });
                }
                await _db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return Result.Ok();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
