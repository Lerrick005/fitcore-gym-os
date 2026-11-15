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

namespace FitCore.Tests.Authentication;

public class AuthenticationServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;
    private readonly Argon2PasswordHasher _hasher = new();

    public AuthenticationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-auth-{Guid.NewGuid():N}.db");
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

    private async Task<(int userId, int roleId)> SeedUserAsync(FitCoreDbContext db, string username, string password, bool active = true)
    {
        var role = new Role { Name = "TestRole" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var (hash, hashParams) = _hasher.Hash(password);
        var user = new User
        {
            Username = username,
            DisplayName = username,
            PasswordHash = hash,
            HashParams = hashParams,
            RoleId = role.Id,
            IsActive = active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, role.Id);
    }

    private Microsoft.Extensions.DependencyInjection.IServiceScopeFactory BuildScopeFactory()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<FitCoreDbContext>(o => o.UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(new SqlitePragmaInterceptor()));
        var sp = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions
            .BuildServiceProvider(services);
        return sp.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
    }

    private CurrentUser NewCurrentUser()
    {
        return new CurrentUser(BuildScopeFactory());
    }

    private AuthenticationService NewService(FitCoreDbContext db, CurrentUser? currentUser = null)
    {
        currentUser ??= NewCurrentUser();
        return new AuthenticationService(db, _hasher, new SystemClock(), currentUser);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Succeeds()
    {
        using var db = NewDb();
        await SeedUserAsync(db, "alice", "Str0ng!Passw0rd");

        var currentUser = NewCurrentUser();
        var svc = NewService(db, currentUser);

        var result = await svc.LoginAsync("alice", "Str0ng!Passw0rd");
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Username.ShouldBe("alice");
        currentUser.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Fails()
    {
        using var db = NewDb();
        await SeedUserAsync(db, "alice", "Str0ng!Passw0rd");

        var svc = NewService(db);
        var result = await svc.LoginAsync("alice", "WrongPassword1!");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("auth.invalid");
    }

    [Fact]
    public async Task Login_WithUnknownUsername_FailsGenerically()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var result = await svc.LoginAsync("nobody", "Str0ng!Passw0rd");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("auth.invalid");
    }

    [Fact]
    public async Task Login_WithInactiveUser_Fails()
    {
        using var db = NewDb();
        await SeedUserAsync(db, "alice", "Str0ng!Passw0rd", active: false);
        var svc = NewService(db);
        var result = await svc.LoginAsync("alice", "Str0ng!Passw0rd");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("auth.inactive");
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccount()
    {
        using var db = NewDb();
        await SeedUserAsync(db, "alice", "Str0ng!Passw0rd");
        var svc = NewService(db);

        for (int i = 0; i < 5; i++)
        {
            var r = await svc.LoginAsync("alice", "Wrong1!");
            r.IsSuccess.ShouldBeFalse();
        }

        // Now locked
        var locked = await svc.LoginAsync("alice", "Str0ng!Passw0rd");
        locked.IsSuccess.ShouldBeFalse();
        locked.ErrorCode.ShouldBe("auth.locked");
    }

    [Fact]
    public async Task Login_SuccessResetsFailedAttempts()
    {
        using var db = NewDb();
        await SeedUserAsync(db, "alice", "Str0ng!Passw0rd");
        var svc = NewService(db);

        await svc.LoginAsync("alice", "Wrong1!");
        await svc.LoginAsync("alice", "Wrong2!");

        var success = await svc.LoginAsync("alice", "Str0ng!Passw0rd");
        success.IsSuccess.ShouldBeTrue();

        var user = await db.Users.FirstAsync(u => u.Username == "alice");
        user.FailedAttempts.ShouldBe(0);
        user.LockedUntilUtc.ShouldBeNull();
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        using var db = NewDb();
        var currentUser = NewCurrentUser();
        currentUser.SetSession(1, "alice", 1);
        currentUser.IsAuthenticated.ShouldBeTrue();

        var svc = NewService(db, currentUser);
        svc.Logout();

        currentUser.IsAuthenticated.ShouldBeFalse();
        currentUser.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task HasPermission_ReturnsTrueForAssignedPermission()
    {
        using var db = NewDb();
        var (userId, roleId) = await SeedUserAsync(db, "alice", "Str0ng!Passw0rd");
        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionKey = "members.view" });
        await db.SaveChangesAsync();

        var currentUser = NewCurrentUser();
        currentUser.SetSession(userId, "alice", roleId);

        currentUser.HasPermission("members.view").ShouldBeTrue();
        currentUser.HasPermission("settings.manage").ShouldBeFalse();
    }

    [Fact]
    public async Task HasPermission_ReturnsFalseWhenNotAuthenticated()
    {
        using var db = NewDb();
        var currentUser = NewCurrentUser();
        currentUser.HasPermission("any").ShouldBeFalse();
    }
}



