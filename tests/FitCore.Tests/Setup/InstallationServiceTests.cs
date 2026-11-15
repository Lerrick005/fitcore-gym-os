using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Setup;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using FitCore.Infrastructure.Setup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Setup;

public class InstallationServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public InstallationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-inst-{Guid.NewGuid():N}.db");
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
    private InstallationService NewService(FitCoreDbContext db)
    {
        var clock = new SystemClock();
        var user = new FakeAlwaysUser();
        var settings = new FitCore.Infrastructure.Settings.SettingsService(db, clock, user);
        var audit = new FitCore.Infrastructure.Services.AuditLogger(db, clock, user);
        var plans = new FitCore.Infrastructure.Plans.MembershipPlanService(db, clock, user, audit);
        var defaults = new FitCore.Infrastructure.Settings.JsonDefaultsProvider(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "defaults", "fitcore.defaults.json"));
        return new InstallationService(db, clock, plans, settings, defaults);
    }

    private sealed class FakeAlwaysUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => true;
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesRow_WithGuidAndFreshStep()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var inst = await svc.GetOrCreateAsync();
        inst.Id.ShouldNotBe(Guid.Empty);
        inst.SetupCompleted.ShouldBeFalse();
        inst.SetupStep.ShouldBe(0);
        inst.SchemaVersion.ShouldBe(1);
        inst.CreatedAtUtc.ShouldNotBe(default);

        (await db.Installations.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_SecondCall_ReturnsSameRow()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var first = await svc.GetOrCreateAsync();
        var second = await svc.GetOrCreateAsync();
        second.Id.ShouldBe(first.Id);
        (await db.Installations.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SetStepAsync_PersistsAndBoundsCheck()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.GetOrCreateAsync();

        // Valid steps: 1..3 (YourGym, Account, Confirm)
        (await svc.SetStepAsync(2)).IsSuccess.ShouldBeTrue();
        (await svc.GetAsync()).SetupStep.ShouldBe(2);

        (await svc.SetStepAsync(-1)).IsSuccess.ShouldBeFalse();
        (await svc.SetStepAsync(4)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CompleteSetupAsync_SetsFlags()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.GetOrCreateAsync();

        (await svc.IsSetupCompletedAsync()).ShouldBeFalse();
        (await svc.CompleteSetupAsync()).IsSuccess.ShouldBeTrue();
        (await svc.IsSetupCompletedAsync()).ShouldBeTrue();

        var inst = await svc.GetAsync();
        inst.SetupCompleted.ShouldBeTrue();
        inst.SetupStep.ShouldBe((int)WizardStep.Confirm);
    }

    [Fact]
    public async Task GetAsync_WithoutRow_Throws()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await Should.ThrowAsync<InvalidOperationException>(() => svc.GetAsync());
    }

    [Fact]
    public async Task SetStepAsync_WithoutRow_ReturnsMissing()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var result = await svc.SetStepAsync(2);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("installation.missing");
    }
}





