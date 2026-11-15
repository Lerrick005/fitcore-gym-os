using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).IsRequired().HasMaxLength(100);
        b.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.HashParams).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> b)
    {
        b.ToTable("UserSessions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
