namespace FitCore.Application.Settings;

/// <summary>
/// Loyalty program configuration (PRS §12.9).
/// Defines how many points are earned per KSh 100 spent, and what they are worth when redeemed.
/// </summary>
public sealed record LoyaltySettings : ISettingsGroup
{
    public static string GroupKey => "loyalty";

    /// <summary>How many points a member earns per 100 KSh spent.</summary>
    public int PointsPerHundredSpent { get; init; } = 1;

    /// <summary>How many points it takes to earn one unit of value (e.g. 100 points = 1 unit).</summary>
    public int PointsPerRedemptionUnit { get; init; } = 100;

    /// <summary>Value of one redemption unit in minor currency units (100.00 KSh = 10000).</summary>
    public long RedemptionUnitValueMinor { get; init; } = 10000;

    /// <summary>Minimum points required to redeem anything.</summary>
    public int MinimumPointsToRedeem { get; init; } = 100;

    /// <summary>If true, points auto-apply as a discount on the next membership sale.</summary>
    public bool AutoApplyOnNextPurchase { get; init; } = false;
}
