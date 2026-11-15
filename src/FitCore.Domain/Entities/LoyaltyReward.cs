namespace FitCore.Domain.Entities;

/// <summary>
/// A reward the gym owner defines. Members earn points through spending;
/// when they have enough, they can redeem for a defined reward.
/// Fully configurable by the owner — no hardcoded tiers.
/// </summary>
public class LoyaltyReward : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Points required to claim this reward.</summary>
    public int PointsRequired { get; set; }

    /// <summary>Reward type: "FreeDays", "FixedDiscount", "PercentageDiscount", "Custom".</summary>
    public string RewardType { get; set; } = "FreeDays";

    /// <summary>
    /// Numeric value depending on type:
    /// FreeDays → days added to membership
    /// FixedDiscount → minor currency units off (e.g. 200.00 KES = 20000)
    /// PercentageDiscount → percentage (e.g. 10 = 10% off)
    /// Custom → 0 (description only)
    /// </summary>
    public long ValueMinor { get; set; }

    /// <summary>If set, reward cannot be redeemed after this date.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
