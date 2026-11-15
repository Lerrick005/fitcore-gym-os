using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class InstallationConfig : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> b)
    {
        b.ToTable("Installation");
        b.HasKey(x => x.Id);
        b.Property(x => x.AppVersionAtCreation).HasMaxLength(50);
    }
}

public class SettingsEntryConfig : IEntityTypeConfiguration<SettingsEntry>
{
    public void Configure(EntityTypeBuilder<SettingsEntry> b)
    {
        b.ToTable("SettingsEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.GroupKey).IsRequired().HasMaxLength(100);
        b.Property(x => x.JsonValue).IsRequired();
        b.HasIndex(x => x.GroupKey).IsUnique();
    }
}

public class ModuleStateConfig : IEntityTypeConfiguration<ModuleState>
{
    public void Configure(EntityTypeBuilder<ModuleState> b)
    {
        b.ToTable("Modules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("Permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(100);
        b.Property(x => x.Category).IsRequired().HasMaxLength(50);
        b.Property(x => x.Description).IsRequired().HasMaxLength(500);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
    }
}

public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermissions");
        b.HasKey(x => new { x.RoleId, x.PermissionKey });
        b.Property(x => x.PermissionKey).IsRequired().HasMaxLength(100);
        b.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLog");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.TimestampUtc);
    }
}

public class LicenceRecordConfig : IEntityTypeConfiguration<LicenceRecord>
{
    public void Configure(EntityTypeBuilder<LicenceRecord> b)
    {
        b.ToTable("LicenceRecord");
        b.HasKey(x => x.Id);
    }
}

public class BackupHistoryConfig : IEntityTypeConfiguration<BackupHistory>
{
    public void Configure(EntityTypeBuilder<BackupHistory> b)
    {
        b.ToTable("BackupHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Path).IsRequired().HasMaxLength(1000);
        b.HasIndex(x => x.TimestampUtc);
    }
}

public class SequenceCounterConfig : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> b)
    {
        b.ToTable("SequenceCounters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(50);
        b.Property(x => x.Prefix).HasMaxLength(20);
        b.HasIndex(x => x.Name).IsUnique();
    }
}
