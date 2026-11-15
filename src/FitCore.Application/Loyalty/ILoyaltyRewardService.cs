using FitCore.Application.Common;

namespace FitCore.Application.Loyalty;

public interface ILoyaltyRewardService
{
    Task<Result<IReadOnlyList<LoyaltyRewardRow>>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<Result<LoyaltyRewardRow>> CreateAsync(LoyaltyRewardInput input, CancellationToken ct = default);
    Task<Result<LoyaltyRewardRow>> UpdateAsync(int id, LoyaltyRewardInput input, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RedeemableReward>>> ListRedeemableForMemberAsync(int memberId, CancellationToken ct = default);

    /// <summary>Redeem a reward for a member. Deducts points and grants the reward.</summary>
    Task<Result> RedeemAsync(int memberId, int rewardId, CancellationToken ct = default);
}
