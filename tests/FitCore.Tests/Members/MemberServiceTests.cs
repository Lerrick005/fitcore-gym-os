using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Members;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Members;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Members;

public class MemberServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public MemberServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-mbr-{Guid.NewGuid():N}.db");
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

    private sealed class DeniedUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => false;
    }

    private MemberService NewService(FitCoreDbContext db, bool permitted = true)
    {
        var clock = new SystemClock();
        var user = permitted ? (FitCore.Application.Abstractions.ICurrentUser)new PermittedUser() : new DeniedUser();
        var audit = new AuditLogger(db, clock, user);
        return new MemberService(db, user, clock, audit);
    }

    private static MemberInput SampleInput(string name = "Alice Smith", string phone = "0712345678") =>
        new(name, phone, null, null, null, null, null);

    [Fact]
    public async Task Create_GeneratesMemberNumber()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var result = await svc.CreateAsync(SampleInput());
        result.IsSuccess.ShouldBeTrue();
        result.Value!.MemberNumber.ShouldBe("MEM-000001");
        result.Value.Status.ShouldBe(MemberStatus.Active);
    }

    [Fact]
    public async Task Create_SequentialNumbers()
    {
        using var db = NewDb();
        var svc = NewService(db);

        var r1 = await svc.CreateAsync(SampleInput("Alice", "0711"));
        var r2 = await svc.CreateAsync(SampleInput("Bob", "0722"));
        var r3 = await svc.CreateAsync(SampleInput("Carol", "0733"));

        r1.Value!.MemberNumber.ShouldBe("MEM-000001");
        r2.Value!.MemberNumber.ShouldBe("MEM-000002");
        r3.Value!.MemberNumber.ShouldBe("MEM-000003");
    }

    [Fact]
    public async Task Create_RejectsMissingFullName()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var result = await svc.CreateAsync(new MemberInput("", "0711", null, null, null, null, null));
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("validation.failed");
    }

    [Fact]
    public async Task Create_RejectsMissingPhone()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var result = await svc.CreateAsync(new MemberInput("Alice", "", null, null, null, null, null));
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("validation.failed");
    }

    [Fact]
    public async Task Create_DeniedWithoutPermission()
    {
        using var db = NewDb();
        var svc = NewService(db, permitted: false);
        var result = await svc.CreateAsync(SampleInput());
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("permission.denied");
    }

    [Fact]
    public async Task Search_ByPartialName_ReturnsMatches()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.CreateAsync(SampleInput("Alice Smith", "0711"));
        await svc.CreateAsync(SampleInput("Bob Jones", "0722"));
        await svc.CreateAsync(SampleInput("Charlie Brown", "0733"));

        var result = await svc.SearchAsync(new MemberSearchQuery { Text = "ali" });
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].FullName.ShouldBe("Alice Smith");
    }

    [Fact]
    public async Task Search_ByPhone_ReturnsMatch()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.CreateAsync(SampleInput("Alice", "0712345678"));
        await svc.CreateAsync(SampleInput("Bob", "0722999999"));

        var result = await svc.SearchAsync(new MemberSearchQuery { Text = "0712345678" });
        result.Value!.Count.ShouldBe(1);
        result.Value[0].FullName.ShouldBe("Alice");
    }

    [Fact]
    public async Task Search_ByMemberNumber_ReturnsMatch()
    {
        using var db = NewDb();
        var svc = NewService(db);
        await svc.CreateAsync(SampleInput("Alice", "0711"));

        var result = await svc.SearchAsync(new MemberSearchQuery { Text = "MEM-000001" });
        result.Value!.Count.ShouldBe(1);
        result.Value[0].MemberNumber.ShouldBe("MEM-000001");
    }

    [Fact]
    public async Task Search_DefaultExcludesArchived()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var created = await svc.CreateAsync(SampleInput("Alice", "0711"));
        await svc.ArchiveAsync(created.Value!.Id, "test", default);

        var defaultSearch = await svc.SearchAsync(new MemberSearchQuery());
        defaultSearch.Value!.Count.ShouldBe(0);

        var archivedSearch = await svc.SearchAsync(new MemberSearchQuery { Status = "archived" });
        archivedSearch.Value!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Archive_SetsArchivedAtAndStatus()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var created = await svc.CreateAsync(SampleInput());
        var id = created.Value!.Id;

        var result = await svc.ArchiveAsync(id, "moved away", default);
        result.IsSuccess.ShouldBeTrue();

        var fetched = await svc.GetAsync(id);
        fetched.Value!.ArchivedAtUtc.ShouldNotBeNull();
        fetched.Value.Status.ShouldBe(MemberStatus.Archived);
    }

    [Fact]
    public async Task Update_ChangesFieldsButNotMemberNumber()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var created = await svc.CreateAsync(SampleInput("Alice", "0711"));
        var id = created.Value!.Id;
        var originalNumber = created.Value.MemberNumber;

        var updateResult = await svc.UpdateAsync(id, new MemberInput(
            "Alice Smith Updated", "0799999999", "alice@example.com", null, null, null, "note"));
        updateResult.IsSuccess.ShouldBeTrue();

        var fetched = await svc.GetAsync(id);
        fetched.Value!.FullName.ShouldBe("Alice Smith Updated");
        fetched.Value.Phone.ShouldBe("0799999999");
        fetched.Value.MemberNumber.ShouldBe(originalNumber);
    }

    [Fact]
    public async Task Update_RejectsArchivedMember()
    {
        using var db = NewDb();
        var svc = NewService(db);
        var created = await svc.CreateAsync(SampleInput());
        await svc.ArchiveAsync(created.Value!.Id, null, default);

        var updateResult = await svc.UpdateAsync(created.Value.Id, SampleInput("New Name", "0799"));
        updateResult.IsSuccess.ShouldBeFalse();
        updateResult.ErrorCode.ShouldBe("member.archived");
    }
}
