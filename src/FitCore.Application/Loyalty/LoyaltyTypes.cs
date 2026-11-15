namespace FitCore.Application.Loyalty;

public sealed record ReferralRow(int Id, string ReferralCode, string ReferrerName, string? RefereeName, DateTime CreatedAt, bool RewardGranted);
public sealed record LoyaltyEntryRow(int Id, string MemberName, string Type, int Points, DateTime OccurredAtUtc, string? Reason);
public sealed record ReferralInput(int ReferrerMemberId, string ReferralCode);
public sealed record LoyaltyEntryInput(int MemberId, string Type, int Points, string? Reason);
