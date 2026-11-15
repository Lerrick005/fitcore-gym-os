using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Memberships;
using FitCore.Application.Settings;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Memberships;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using FitCore.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Memberships;

public class MembershipServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public MembershipServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-mem-{Guid.NewGuid():N}.db");
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

    private sealed class PermittedUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => true;
    }

    private MembershipService NewService(FitCoreDbContext db)
    {
        var clock = new SystemClock();
        var user = new PermittedUser();
        var audit = new AuditLogger(db, clock, user);
        var settings = new SettingsService(db, clock, user);
        return new MembershipService(db, user, clock, audit, settings);
    }

    private async Task<int> SeedMemberAsync(FitCoreDbContext db, string name = "Alice")
    {
        var m = new Member
        {
            MemberNumber = $"MEM-{Guid.NewGuid().ToString("N")[..6]}",
            FullName = name,
            Phone = "0712345678",
            Status = MemberStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Members.Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    }

    private async Task<int> SeedPlanAsync(FitCoreDbContext db, string name = "Monthly", long priceMinor = 350000, int duration = 1, DurationUnit unit = DurationUnit.Months)
    {
        var p = new MembershipPlan
        {
            Name = name,
            PriceMinor = priceMinor,
            CurrencyCode = "KES",
            DurationValue = duration,
            DurationUnit = unit,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.MembershipPlans.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    }

    [Fact]
    public async Task Sell_SnapshotsPlanValues()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db, "Monthly", 350000, 1, DurationUnit.Months);
        var svc = NewService(db);

        var result = await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));
        result.IsSuccess.ShouldBeTrue();
        result.Value!.PlanNameSnapshot.ShouldBe("Monthly");
        result.Value.PriceMinorSnapshot.ShouldBe(350000);
        result.Value.CurrencyCodeSnapshot.ShouldBe("KES");
        result.Value.Status.ShouldBe(MembershipStatus.Active);
    }

    [Fact]
    public async Task Sell_PlanEditAfterward_DoesNotAffectSnapshot()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db, "Monthly", 350000);
        var svc = NewService(db);

        var sold = await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));

        // Edit plan price
        var plan = await db.MembershipPlans.FirstAsync(p => p.Id == planId);
        plan.PriceMinor = 500000;
        await db.SaveChangesAsync();

        // Re-read membership
        var reloaded = await db.Memberships.FirstAsync(m => m.Id == sold.Value!.Id);
        reloaded.PriceMinorSnapshot.ShouldBe(350000);  // unchanged
        reloaded.PlanNameSnapshot.ShouldBe("Monthly");
    }

    [Fact]
    public async Task Sell_BlockedIfActiveMembershipExists()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var svc = NewService(db);

        (await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null))).IsSuccess.ShouldBeTrue();

        var second = await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));
        second.IsSuccess.ShouldBeFalse();
        second.ErrorCode.ShouldBe("membership.active_exists");
    }

    [Fact]
    public async Task Sell_EndDateMatchesDuration()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db, duration: 3, unit: DurationUnit.Months);
        var svc = NewService(db);

        var start = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = await svc.SellAsync(new SellMembershipRequest(memberId, planId, start, null));

        result.Value!.StartDate.ShouldBe(start);
        result.Value.EndDate.ShouldBe(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Sell_RejectsArchivedMember()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var member = await db.Members.FirstAsync(m => m.Id == memberId);
        member.ArchivedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var svc = NewService(db);
        var result = await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("member.archived");
    }

    [Fact]
    public async Task Freeze_ExtendsEndDate()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var svc = NewService(db);

        var sold = await svc.SellAsync(new SellMembershipRequest(memberId, planId, DateTime.UtcNow.Date, null));
        var originalEnd = sold.Value!.EndDate;

        var frozen = await svc.FreezeAsync(sold.Value.Id,
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(12), "vacation", default);

        frozen.IsSuccess.ShouldBeTrue();
        frozen.Value!.Status.ShouldBe(MembershipStatus.Frozen);
        frozen.Value.EndDate.ShouldBe(originalEnd.AddDays(7));
    }

    [Fact]
    public async Task Cancel_SetsStatus()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var svc = NewService(db);

        var sold = await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));
        var result = await svc.CancelAsync(sold.Value!.Id, "change of plans", default);
        result.IsSuccess.ShouldBeTrue();

        var reloaded = await db.Memberships.FirstAsync(m => m.Id == sold.Value.Id);
        reloaded.Status.ShouldBe(MembershipStatus.Cancelled);
        reloaded.CancelledAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetActiveForMember_ReturnsActive()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var svc = NewService(db);

        await svc.SellAsync(new SellMembershipRequest(memberId, planId, null, null));
        var active = await svc.GetActiveForMemberAsync(memberId);

        active.IsSuccess.ShouldBeTrue();
        active.Value!.Status.ShouldBe(MembershipStatus.Active);
    }

    [Fact]
    public async Task GetActiveForMember_AutoExpiresWhenPastEndDate()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db, duration: 1, unit: DurationUnit.Months);
        var svc = NewService(db);

        // Sell with past start date so end date is also past
        var past = DateTime.UtcNow.Date.AddMonths(-3);
        await svc.SellAsync(new SellMembershipRequest(memberId, planId, past, null));

        var result = await svc.GetActiveForMemberAsync(memberId);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("membership.none_active");

        var reloaded = await db.Memberships.FirstAsync();
        reloaded.Status.ShouldBe(MembershipStatus.Expired);
    }

    [Fact]
    public async Task ListForMember_ReturnsAllOrderedByDate()
    {
        using var db = NewDb();
        var memberId = await SeedMemberAsync(db);
        var planId = await SeedPlanAsync(db);
        var svc = NewService(db);

        await svc.SellAsync(new SellMembershipRequest(memberId, planId, DateTime.UtcNow.Date.AddMonths(-5), null));

        var list = await svc.ListForMemberAsync(memberId);
        list.IsSuccess.ShouldBeTrue();
        list.Value!.Count.ShouldBe(1);
    }
}
