using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Modules;
using FitCore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Modules;

public class ModuleAccessGuardTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public ModuleAccessGuardTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-guard-{Guid.NewGuid():N}.db");
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
    public async Task UnknownKey_ReturnsFalse_And_UnknownError()
    {
        using var db = NewDb();
        var guard = new ModuleAccessGuard(db, new ModuleRegistry());

        (await guard.IsEnabledAsync("nonsense")).ShouldBeFalse();
        var r = await guard.RequireAsync("nonsense");
        r.IsSuccess.ShouldBeFalse();
        r.ErrorCode.ShouldBe("module.unknown");
    }

    [Fact]
    public async Task KnownKey_NoRow_ReturnsFalse_And_Disabled()
    {
        using var db = NewDb();
        var guard = new ModuleAccessGuard(db, new ModuleRegistry());

        (await guard.IsEnabledAsync("trainers")).ShouldBeFalse();
        var r = await guard.RequireAsync("trainers");
        r.IsSuccess.ShouldBeFalse();
        r.ErrorCode.ShouldBe("module.disabled");
    }

    [Fact]
    public async Task KnownKey_Enabled_ReturnsTrue_And_Ok()
    {
        using var db = NewDb();
        db.Modules.Add(new ModuleState { Key = "trainers", IsEnabled = true, EnabledAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var guard = new ModuleAccessGuard(db, new ModuleRegistry());
        (await guard.IsEnabledAsync("trainers")).ShouldBeTrue();
        (await guard.RequireAsync("trainers")).IsSuccess.ShouldBeTrue();
    }
}
