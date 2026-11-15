using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Authorization;
using FitCore.Infrastructure.Authorization;
using FitCore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Authorization;

public class RoleSeederTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public RoleSeederTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-seed-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<FitCoreDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(new SqlitePragmaInterceptor())
            .Options;
        using var db = new FitCoreDbContext(_options);
        db.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private FitCoreDbContext NewDb() => new(_options);

    [Fact]
    public async Task Seed_OnEmptyDb_AddsAllPermissionsAndRoles()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        var result = await seeder.SeedAsync();
        result.IsSuccess.ShouldBeTrue();

        var permCount = await db.Permissions.AsQueryable().CountAsync();
        var roleCount = await db.Roles.AsQueryable().CountAsync();

        permCount.ShouldBe(PermissionCatalogue.Count);
        roleCount.ShouldBe(DefaultRoleCatalogue.All.Count);
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        (await db.Permissions.AsQueryable().CountAsync()).ShouldBe(PermissionCatalogue.Count);
        (await db.Roles.AsQueryable().CountAsync()).ShouldBe(DefaultRoleCatalogue.All.Count);
    }

    [Fact]
    public async Task OwnerRole_HasAllPermissions()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        await seeder.SeedAsync();

        var owner = await db.Roles.AsQueryable().FirstAsync(r => r.IsOwnerRole);
        var ownerPermCount = await db.RolePermissions.AsQueryable().CountAsync(rp => rp.RoleId == owner.Id);

        ownerPermCount.ShouldBe(PermissionCatalogue.Count);
    }

    [Fact]
    public async Task ManagerRole_LacksAdministrativePermissions()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        await seeder.SeedAsync();

        var manager = await db.Roles.AsQueryable().FirstAsync(r => r.Name == "Manager");
        var perms = await db.RolePermissions.AsQueryable()
            .Where(rp => rp.RoleId == manager.Id)
            .Select(rp => rp.PermissionKey)
            .ToListAsync();

        perms.ShouldNotContain("users.manage");
        perms.ShouldNotContain("roles.manage");
        perms.ShouldNotContain("branding.manage");
        perms.ShouldNotContain("modules.manage");
        perms.ShouldNotContain("settings.manage");
        perms.ShouldContain("members.view");
        perms.ShouldContain("payments.record");
    }

    [Fact]
    public async Task ReceptionistRole_HasOnlyFrontDeskPermissions()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        await seeder.SeedAsync();

        var receptionist = await db.Roles.AsQueryable().FirstAsync(r => r.Name == "Receptionist");
        var perms = await db.RolePermissions.AsQueryable()
            .Where(rp => rp.RoleId == receptionist.Id)
            .Select(rp => rp.PermissionKey)
            .ToListAsync();

        perms.ShouldContain("members.view");
        perms.ShouldContain("payments.record");
        perms.ShouldContain("attendance.checkin");
        perms.ShouldNotContain("payments.refund");
        perms.ShouldNotContain("users.manage");
        perms.ShouldNotContain("settings.manage");
    }

    [Fact]
    public async Task RoleSeeder_DoesNotOverwriteUserEdits()
    {
        using var db = NewDb();
        var seeder = new RoleSeeder(db);
        await seeder.SeedAsync();

        // Simulate owner removing a permission from Receptionist
        var receptionist = await db.Roles.AsQueryable().FirstAsync(r => r.Name == "Receptionist");
        var target = await db.RolePermissions.AsQueryable()
            .FirstAsync(rp => rp.RoleId == receptionist.Id && rp.PermissionKey == "reports.view");
        db.RolePermissions.Remove(target);
        await db.SaveChangesAsync();

        // Re-seed
        await seeder.SeedAsync();

        var stillGone = await db.RolePermissions.AsQueryable()
            .AnyAsync(rp => rp.RoleId == receptionist.Id && rp.PermissionKey == "reports.view");
        stillGone.ShouldBeFalse();
    }
}
