using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class RecoveryAttemptConfig : IEntityTypeConfiguration<RecoveryAttempt>
{
    public void Configure(EntityTypeBuilder<RecoveryAttempt> b)
    {
        b.ToTable("RecoveryAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.ChallengeId).IsRequired().HasMaxLength(64);
        b.Property(x => x.InstallationId).IsRequired().HasMaxLength(64);
        b.HasIndex(x => x.ChallengeId).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
