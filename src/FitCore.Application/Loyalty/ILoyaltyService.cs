using FitCore.Application.Common;

namespace FitCore.Application.Loyalty;

public interface ILoyaltyService
{
    Task<Result<IReadOnlyList<ReferralRow>>> ListReferralsAsync(CancellationToken ct = default);
    Task<Result<ReferralRow>> CreateReferralAsync(ReferralInput input, CancellationToken ct = default);
    Task<Result> GrantReferralRewardAsync(int referralId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<LoyaltyEntryRow>>> ListEntriesAsync(int? memberId, CancellationToken ct = default);
    Task<Result<LoyaltyEntryRow>> AddEntryAsync(LoyaltyEntryInput input, CancellationToken ct = default);

    /// <summary>Current points balance for a member (sum of earn - redeem + adjustments).</summary>
    Task<Result<int>> GetBalanceAsync(int memberId, CancellationToken ct = default);

    /// <summary>Value in minor currency units if the member redeemed all their points now.</summary>
    Task<Result<long>> GetBalanceValueMinorAsync(int memberId, CancellationToken ct = default);
}

