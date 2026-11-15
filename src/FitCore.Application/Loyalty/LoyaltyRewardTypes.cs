namespace FitCore.Application.Loyalty;

public sealed record LoyaltyRewardRow(
    int Id,
    string Name,
    string? Description,
    int PointsRequired,
    string RewardType,
    long ValueMinor,
    DateTime? ExpiresAtUtc,
    bool IsActive);

public sealed record LoyaltyRewardInput(
    string Name,
    string? Description,
    int PointsRequired,
    string RewardType,
    long ValueMinor,
    DateTime? ExpiresAtUtc,
    bool IsActive);

public sealed record RedeemableReward(
    int RewardId,
    string Name,
    string? Description,
    int PointsRequired,
    string RewardType,
    long ValueMinor,
    bool CanAfford,
    string ValueDisplay);
