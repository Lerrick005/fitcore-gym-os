using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Dashboard;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using FitCore.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Dashboard;

public class DashboardServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public DashboardServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-dash-{Guid.NewGuid():N}.db");
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

    private DashboardService NewService(FitCoreDbContext db)
    {
        var clock = new SystemClock();
        var settings = new SettingsService(db, clock, new FakeSettingsUser());
        return new DashboardService(db, settings, clock);
    }

    private sealed class FakeSettingsUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => true;
    }

    [Fact]
    public async Task GetStats_EmptyDb_ReturnsZeroes()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var stats = await svc.GetStatsAsync();

        stats.ActiveMembers.ShouldBe(0);
        stats.TotalMembers.ShouldBe(0);
        stats.ExpiringSoonMembers.ShouldBe(0);
        stats.ExpiredMembers.ShouldBe(0);
        stats.TodayCheckIns.ShouldBe(0);
        stats.TodayRevenueMinor.ShouldBe(0);
        stats.OutstandingBalancesCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetStats_WithActiveMember_CountsIt()
    {
        using var db = NewDb();
        db.Members.Add(new Member
        {
            MemberNumber = "M-001",
            FullName = "Alice",
            Phone = "0700000000",
            Status = MemberStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var svc = NewService(db);
        var stats = await svc.GetStatsAsync();

        stats.ActiveMembers.ShouldBe(1);
        stats.TotalMembers.ShouldBe(1);
    }
}
