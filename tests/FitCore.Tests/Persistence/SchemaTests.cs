using System;
using System.IO;
using FitCore.Domain.Entities;
using FitCore.Domain.Enums;
using FitCore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Persistence;

public class SchemaTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<FitCoreDbContext> _options;

    public SchemaTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fitcore-test-{Guid.NewGuid():N}.db");
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

    [Fact]
    public void Schema_Has_33_Tables()
    {
        using var db = new FitCoreDbContext(_options);
        using var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%' ORDER BY name;";
        using var rdr = cmd.ExecuteReader();
        var tables = new System.Collections.Generic.List<string>();
        while (rdr.Read()) tables.Add(rdr.GetString(0));
        tables.Count.ShouldBe(35, $"Tables found: {string.Join(", ", tables)}");
        tables.ShouldContain("Payments");
        tables.ShouldContain("Receipts");
        tables.ShouldContain("RecoveryAttempts");
        tables.ShouldContain("LoyaltyRewards");
    }

    [Fact]
    public void Schema_Has_4_AppendOnly_Triggers()
    {
        using var db = new FitCoreDbContext(_options);
        using var conn = db.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='trigger' ORDER BY name;";
        using var rdr = cmd.ExecuteReader();
        var triggers = new System.Collections.Generic.List<string>();
        while (rdr.Read()) triggers.Add(rdr.GetString(0));

        triggers.Count.ShouldBe(4, $"Triggers found: {string.Join(", ", triggers)}");
        triggers.ShouldContain("trg_Payments_NoUpdate");
        triggers.ShouldContain("trg_Payments_NoDelete");
        triggers.ShouldContain("trg_Receipts_NoUpdate");
        triggers.ShouldContain("trg_Receipts_NoDelete");
    }

    [Fact]
    public void Payment_Update_Is_Blocked_By_Trigger()
    {
        // Insert minimal rows directly so FKs don't block us.
        using var db = new FitCoreDbContext(_options);
        var member = new Member
        {
            MemberNumber = "M-001",
            FullName = "Test Member",
            Phone = "0700000000",
            Status = MemberStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Members.Add(member);

        var plan = new MembershipPlan
        {
            Name = "Monthly",
            PriceMinor = 300000,
            CurrencyCode = "KES",
            DurationValue = 1,
            DurationUnit = DurationUnit.Months,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.MembershipPlans.Add(plan);
        db.SaveChanges();

        var membership = new Membership
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            PlanNameSnapshot = "Monthly",
            PriceMinorSnapshot = 300000,
            CurrencyCodeSnapshot = "KES",
            DurationSnapshot = 30,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MembershipStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Memberships.Add(membership);
        db.SaveChanges();

        var payment = new Payment
        {
            MemberId = member.Id,
            MembershipId = membership.Id,
            AmountMinor = 300000,
            CurrencyCode = "KES",
            Method = "Cash",
            ReceivedAtUtc = DateTime.UtcNow,
            ReceivedBy = 1,
            Status = PaymentStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        db.SaveChanges();

        var ex = Should.Throw<SqliteException>(() =>
        {
            using var conn = db.Database.GetDbConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Payments SET AmountMinor = 0 WHERE Id = $id;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$id";
            p.Value = payment.Id;
            cmd.Parameters.Add(p);
            cmd.ExecuteNonQuery();
        });
        ex.SqliteErrorCode.ShouldBe(19);  // SQLITE_CONSTRAINT
        ex.Message.ShouldContain("append-only");
    }

    [Fact]
    public void Receipt_Delete_Is_Blocked_By_Trigger()
    {
        using var db = new FitCoreDbContext(_options);
        var member = new Member
        {
            MemberNumber = "M-002",
            FullName = "Test Member 2",
            Phone = "0700000001",
            Status = MemberStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Members.Add(member);
        db.SaveChanges();

        var payment = new Payment
        {
            MemberId = member.Id,
            AmountMinor = 1000,
            CurrencyCode = "KES",
            Method = "Cash",
            ReceivedAtUtc = DateTime.UtcNow,
            ReceivedBy = 1,
            Status = PaymentStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        db.SaveChanges();

        var receipt = new Receipt
        {
            ReceiptNumber = "R-000001",
            PaymentId = payment.Id,
            IssuedAtUtc = DateTime.UtcNow,
            IssuedBy = 1,
            SnapshotJson = "{}"
        };
        db.Receipts.Add(receipt);
        db.SaveChanges();

        var ex = Should.Throw<SqliteException>(() =>
        {
            using var conn = db.Database.GetDbConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Receipts WHERE Id = $id;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$id";
            p.Value = receipt.Id;
            cmd.Parameters.Add(p);
            cmd.ExecuteNonQuery();
        });
        ex.SqliteErrorCode.ShouldBe(19);  // SQLITE_CONSTRAINT
        ex.Message.ShouldContain("append-only");
    }
}


