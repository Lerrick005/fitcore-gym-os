using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class TrainerConfig : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> b)
    {
        b.ToTable("Trainers");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
    }
}

public class TrainerAssignmentConfig : IEntityTypeConfiguration<TrainerAssignment>
{
    public void Configure(EntityTypeBuilder<TrainerAssignment> b)
    {
        b.ToTable("TrainerAssignments");
        b.HasKey(x => x.Id);
        b.HasOne<Trainer>().WithMany().HasForeignKey(x => x.TrainerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProgrammeConfig : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> b)
    {
        b.ToTable("Programmes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
    }
}

public class ProgrammeAssignmentConfig : IEntityTypeConfiguration<ProgrammeAssignment>
{
    public void Configure(EntityTypeBuilder<ProgrammeAssignment> b)
    {
        b.ToTable("ProgrammeAssignments");
        b.HasKey(x => x.Id);
        b.HasOne<Programme>().WithMany().HasForeignKey(x => x.ProgrammeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProgressEntryConfig : IEntityTypeConfiguration<ProgressEntry>
{
    public void Configure(EntityTypeBuilder<ProgressEntry> b)
    {
        b.ToTable("ProgressEntries");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.MemberId, x.RecordedAtUtc });
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryItemConfig : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("InventoryItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Sku).HasMaxLength(100);
        b.Property(x => x.CurrencyCode).HasMaxLength(3);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockMovementConfig : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements");
        b.HasKey(x => x.Id);
        b.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupplierConfig : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Suppliers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
    }
}

public class ExpenseCategoryConfig : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> b)
    {
        b.ToTable("ExpenseCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
    }
}

public class ExpenseConfig : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("Expenses");
        b.HasKey(x => x.Id);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.HasIndex(x => x.OccurredAtUtc);
        b.HasOne<ExpenseCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class FollowUpConfig : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> b)
    {
        b.ToTable("FollowUps");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.MemberId, x.Status });
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReferralConfig : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> b)
    {
        b.ToTable("Referrals");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferralCode).HasMaxLength(50);
        b.HasIndex(x => x.ReferralCode);
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.ReferrerMemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.RefereeMemberId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LoyaltyLedgerEntryConfig : IEntityTypeConfiguration<LoyaltyLedgerEntry>
{
    public void Configure(EntityTypeBuilder<LoyaltyLedgerEntry> b)
    {
        b.ToTable("LoyaltyLedger");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.MemberId, x.OccurredAtUtc });
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}
