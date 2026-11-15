using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Identity;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Identity;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Identity;

public class UserManagementTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;
    private readonly Argon2PasswordHasher _hasher = new();

    public UserManagementTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-usrmgmt-{Guid.NewGuid():N}.db");
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

    private Microsoft.Extensions.DependencyInjection.IServiceScopeFactory BuildScopeFactory()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<FitCoreDbContext>(o => o.UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(new SqlitePragmaInterceptor()));
        var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions
            .BuildServiceProvider(services);
        return sp.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
    }

    private UserManagementService NewService(FitCoreDbContext db)
    {
        var clock = new SystemClock();
        var audit = new AuditLogger(db, clock, new CurrentUser(BuildScopeFactory()));
        return new UserManagementService(db, _hasher, clock, audit, new AlwaysUser());
    }

    private sealed class AlwaysUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => true;
    }

    private async Task<(int ownerRoleId, int staffRoleId)> SeedRolesAsync(FitCoreDbContext db)
    {
        var ownerRole = new Role { Name = "Owner", IsOwnerRole = true, IsSystemDefault = true };
        var staffRole = new Role { Name = "Receptionist", IsSystemDefault = true };
        db.Roles.Add(ownerRole);
        db.Roles.Add(staffRole);
        await db.SaveChangesAsync();
        return (ownerRole.Id, staffRole.Id);
    }

    private async Task<User> SeedOwnerAsync(FitCoreDbContext db, int ownerRoleId, string username)
    {
        var (hash, hp) = _hasher.Hash("Str0ng!Passw0rd");
        var user = new User
        {
            Username = username,
            DisplayName = username,
            PasswordHash = hash,
            HashParams = hp,
            RoleId = ownerRoleId,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CannotDeactivate_LastActiveOwner()
    {
        using var db = NewDb();
        var (ownerRoleId, _) = await SeedRolesAsync(db);
        var owner = await SeedOwnerAsync(db, ownerRoleId, "owner1");
        var svc = NewService(db);

        var result = await svc.SetActiveAsync(owner.Id, false, "test", default);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("identity.owner.last");
    }

    [Fact]
    public async Task CannotDemote_LastActiveOwner()
    {
        using var db = NewDb();
        var (ownerRoleId, staffRoleId) = await SeedRolesAsync(db);
        var owner = await SeedOwnerAsync(db, ownerRoleId, "owner1");
        var svc = NewService(db);

        var result = await svc.ChangeRoleAsync(owner.Id, staffRoleId, "test", default);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("identity.owner.last");
    }

    [Fact]
    public async Task CanDeactivateOwner_WhenSecondActiveOwnerExists()
    {
        using var db = NewDb();
        var (ownerRoleId, _) = await SeedRolesAsync(db);
        var owner1 = await SeedOwnerAsync(db, ownerRoleId, "owner1");
        var owner2 = await SeedOwnerAsync(db, ownerRoleId, "owner2");
        var svc = NewService(db);

        var result = await svc.SetActiveAsync(owner1.Id, false, "test", default);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangePassword_RequiresCorrectCurrentPassword()
    {
        using var db = NewDb();
        var (ownerRoleId, _) = await SeedRolesAsync(db);
        var owner = await SeedOwnerAsync(db, ownerRoleId, "owner1");
        var svc = NewService(db);

        var wrongCurrent = await svc.ChangePasswordAsync(owner.Id, "WrongOldPass1!", "N3w!Passw0rd", default);
        wrongCurrent.IsSuccess.ShouldBeFalse();
        wrongCurrent.ErrorCode.ShouldBe("auth.current_password.invalid");

        var valid = await svc.ChangePasswordAsync(owner.Id, "Str0ng!Passw0rd", "N3w!Passw0rd", default);
        valid.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangePassword_RejectsWeakNewPassword()
    {
        using var db = NewDb();
        var (ownerRoleId, _) = await SeedRolesAsync(db);
        var owner = await SeedOwnerAsync(db, ownerRoleId, "owner1");
        var svc = NewService(db);

        var result = await svc.ChangePasswordAsync(owner.Id, "Str0ng!Passw0rd", "weak", default);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("identity.password.weak");
    }
}



