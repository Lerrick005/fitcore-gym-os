using FitCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitCore.Infrastructure.Persistence.Configurations;

public class LoyaltyRewardConfig : IEntityTypeConfiguration<LoyaltyReward>
{
    public void Configure(EntityTypeBuilder<LoyaltyReward> b)
    {
        b.ToTable("LoyaltyRewards");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RewardType).IsRequired().HasMaxLength(50);
        b.HasIndex(x => new { x.IsActive, x.PointsRequired });
    }
}
