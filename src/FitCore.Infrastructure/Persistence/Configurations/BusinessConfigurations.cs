using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class MemberConfig : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.ToTable("Members");
        b.HasKey(x => x.Id);
        b.Property(x => x.MemberNumber).IsRequired().HasMaxLength(50);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Phone).IsRequired().HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Gender).HasMaxLength(20);
        b.Property(x => x.EmergencyContact).HasMaxLength(300);
        b.Property(x => x.PhotoPath).HasMaxLength(1000);
        b.Property(x => x.ConsentVersion).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasIndex(x => x.MemberNumber).IsUnique();
        b.HasIndex(x => x.FullName);
        b.HasIndex(x => x.Phone);
    }
}

public class MembershipPlanConfig : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> b)
    {
        b.ToTable("MembershipPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.JoiningFeeCurrencyCode).HasMaxLength(3);
        b.Property(x => x.Category).IsRequired().HasMaxLength(50).HasDefaultValue("Membership");
        b.Property(x => x.IsDefault).HasDefaultValue(false);
    }
}

public class MembershipConfig : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> b)
    {
        b.ToTable("Memberships");
        b.HasKey(x => x.Id);
        b.Property(x => x.PlanNameSnapshot).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCodeSnapshot).IsRequired().HasMaxLength(3);
        b.HasIndex(x => new { x.EndDate, x.Status });
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MembershipPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Discount>().WithMany().HasForeignKey(x => x.DiscountId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DiscountConfig : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> b)
    {
        b.ToTable("Discounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50);
        b.HasIndex(x => x.Code);
    }
}

public class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.Method).IsRequired().HasMaxLength(50);
        b.Property(x => x.Reference).HasMaxLength(100);
        b.HasIndex(x => x.ReceivedAtUtc);
        b.HasIndex(x => x.Method);
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Membership>().WithMany().HasForeignKey(x => x.MembershipId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Payment>().WithMany().HasForeignKey(x => x.RelatedPaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReceiptConfig : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> b)
    {
        b.ToTable("Receipts");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(50);
        b.Property(x => x.SnapshotJson).IsRequired();
        b.HasIndex(x => x.ReceiptNumber).IsUnique();
        b.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Receipt>().WithMany().HasForeignKey(x => x.VoidedByReceiptId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfig : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("AttendanceRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.CorrectionReason).HasMaxLength(500);
        b.HasIndex(x => new { x.MemberId, x.CheckInUtc });
        b.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayOverrideConfig : IEntityTypeConfiguration<HolidayOverride>
{
    public void Configure(EntityTypeBuilder<HolidayOverride> b)
    {
        b.ToTable("HolidayOverrides");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).HasMaxLength(200);
        b.HasIndex(x => x.Date).IsUnique();
    }
}

