using System;
using System.IO;
using System.Threading.Tasks;
using FitCore.Application.Settings;
using FitCore.Infrastructure.Formatting;
using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
using FitCore.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Formatting;

public class FormatterTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public FormatterTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-fmt-{Guid.NewGuid():N}.db");
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

    private async Task SeedLocaleAsync(FitCoreDbContext db, LocaleSettings locale)
    {
        var svc = new SettingsService(db, new SystemClock(), new AlwaysPermittedUser());
        var r = await svc.SaveAsync(locale);
        r.IsSuccess.ShouldBeTrue();
    }

    private sealed class AlwaysPermittedUser : FitCore.Application.Abstractions.ICurrentUser
    {
        public bool IsAuthenticated => true;
        public int? UserId => 1;
        public string? Username => "test";
        public int? RoleId => 1;
        public bool HasPermission(string key) => true;
    }

    [Fact]
    public async Task CurrencyFormatter_KenyanShillings_FormatsCorrectly()
    {
        using var db = NewDb();
        await SeedLocaleAsync(db, new LocaleSettings
        {
            CurrencyCode = "KES",
            CurrencySymbol = "KSh",
            SymbolBeforeAmount = true,
            DecimalPlaces = 2,
            ThousandsSeparator = ",",
            DecimalSeparator = ".",
            TimeZoneId = "Africa/Nairobi"
        });

        var svc = new SettingsService(db, new SystemClock(), new AlwaysPermittedUser());
        var fmt = new LocaleCurrencyFormatter(svc);

        var s = await fmt.FormatAsync(350000L, "KES");
        s.ShouldBe("KSh 3,500.00");
    }

    [Fact]
    public async Task DateTimeFormatter_NairobiTime_ConvertsFromUtc()
    {
        using var db = NewDb();
        await SeedLocaleAsync(db, new LocaleSettings
        {
            CurrencyCode = "KES",
            CurrencySymbol = "KSh",
            TimeZoneId = "Africa/Nairobi",
            DateFormat = "dd/MM/yyyy",
            TimeFormat = "HH:mm"
        });

        var svc = new SettingsService(db, new SystemClock(), new AlwaysPermittedUser());
        var fmt = new LocaleDateTimeFormatter(svc);

        // 2026-06-15 09:30 UTC = 2026-06-15 12:30 Africa/Nairobi (UTC+3)
        var utc = new DateTime(2026, 6, 15, 9, 30, 0, DateTimeKind.Utc);
        var dateStr = await fmt.FormatDateAsync(utc);
        var timeStr = await fmt.FormatTimeAsync(utc);

        dateStr.ShouldBe("15/06/2026");
        timeStr.ShouldBe("12:30");
    }

    [Fact]
    public async Task BusinessDayStart_NairobiMidnight_IsUtc21()
    {
        using var db = NewDb();
        await SeedLocaleAsync(db, new LocaleSettings
        {
            CurrencyCode = "KES",
            CurrencySymbol = "KSh",
            TimeZoneId = "Africa/Nairobi"
        });

        var svc = new SettingsService(db, new SystemClock(), new AlwaysPermittedUser());
        var fmt = new LocaleDateTimeFormatter(svc);

        // 2026-06-15 12:00 UTC = 15:00 Nairobi; business day start = 2026-06-15 00:00 Nairobi = 2026-06-14 21:00 UTC
        var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var dayStart = await fmt.GetBusinessDayStartUtcAsync(utc);

        dayStart.ShouldBe(new DateTime(2026, 6, 14, 21, 0, 0, DateTimeKind.Utc));
    }
}
