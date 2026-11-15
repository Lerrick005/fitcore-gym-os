using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Settings;
using FitCore.Infrastructure.Identity;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using FitCore.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Settings;

/// <summary>Fake ICurrentUser with explicit permission set for tests.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _permissions;
    public FakeCurrentUser(params string[] permissions) => _permissions = new(permissions);
    public bool IsAuthenticated => true;
    public int? UserId => 42;
    public string? Username => "test-user";
    public int? RoleId => 1;
    public bool HasPermission(string permissionKey) => _permissions.Contains(permissionKey);
}

public class SettingsServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public SettingsServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-set-{Guid.NewGuid():N}.db");
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

    private SettingsService NewService(
        FitCoreDbContext db,
        ICurrentUser? user = null) =>
        new(db, new SystemClock(), user ?? new FakeCurrentUser("settings.manage"));

    [Fact]
    public async Task GetAsync_OnEmptyDb_ReturnsDefaults_NoRowWritten()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var s = await svc.GetAsync<BrandingSettings>();
        s.BusinessName.ShouldBe(string.Empty);
        (await db.SettingsEntries.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_Twice_ReturnsSameInstance()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var a = await svc.GetAsync<BrandingSettings>();
        var b = await svc.GetAsync<BrandingSettings>();
        ReferenceEquals(a, b).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_Persists_RaisesChanged()
    {
        using var db = NewDb();
        var svc = NewService(db);

        SettingsChangedEventArgs? fired = null;
        svc.Changed += (_, args) => fired = args;

        var value = new BrandingSettings { BusinessName = "Test Gym" };
        var result = await svc.SaveAsync(value);

        result.IsSuccess.ShouldBeTrue();
        fired.ShouldNotBeNull();
        fired!.GroupKey.ShouldBe("branding");
        fired.NewValue.ShouldBeOfType<BrandingSettings>().BusinessName.ShouldBe("Test Gym");

        (await db.SettingsEntries.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_FirstSave_AuditsWithNullOld()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.SaveAsync(new BrandingSettings { BusinessName = "Gym A" });

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.ShouldBe("settings.save");
        audit.EntityType.ShouldBe("SettingsGroup");
        audit.EntityId.ShouldBe("branding");
        audit.OldValue.ShouldBeNull();
        audit.NewValue.ShouldNotBeNull().ShouldContain("Gym A");
    }

    [Fact]
    public async Task SaveAsync_SecondSave_AuditsOldAndNew()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.SaveAsync(new BrandingSettings { BusinessName = "Gym A" });
        await svc.SaveAsync(new BrandingSettings { BusinessName = "Gym B" });

        var audits = await db.AuditLogs.OrderBy(a => a.Id).ToListAsync();
        audits.Count.ShouldBe(2);
        audits[1].OldValue.ShouldNotBeNull().ShouldContain("Gym A");
        audits[1].NewValue.ShouldNotBeNull().ShouldContain("Gym B");
    }

    [Fact]
    public async Task SaveAsync_NoOp_SkipsAudit()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var value = new BrandingSettings { BusinessName = "Gym A" };
        await svc.SaveAsync(value);
        await svc.SaveAsync(value);

        (await db.AuditLogs.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        using var db = NewDb();
        // Real stub current user => not authenticated, no permissions.
        var svc = new SettingsService(db, new SystemClock(), new FakeCurrentUser());

        var result = await svc.SaveAsync(new BrandingSettings { BusinessName = "Gym A" });
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("permission.denied");
        (await db.SettingsEntries.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task SaveAsync_InsideSystemContext_Succeeds()
    {
        using var db = NewDb();
        var svc = new SettingsService(db, new SystemClock(), new FakeCurrentUser());

        using (SettingsSystemContext.Enter())
        {
            var result = await svc.SaveAsync(new BrandingSettings { BusinessName = "Wizard Gym" });
            result.IsSuccess.ShouldBeTrue();
        }

        var audit = await db.AuditLogs.SingleAsync();
        audit.Reason.ShouldBe("wizard");
        audit.UserId.ShouldBeNull();
    }
}

