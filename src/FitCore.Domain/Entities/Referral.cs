namespace FitCore.Domain.Entities;

public class Referral : AuditableEntity
{
    public int ReferrerMemberId { get; set; }
    public string? ReferralCode { get; set; }
    public int? RefereeMemberId { get; set; }
    public DateTime ReferredAtUtc { get; set; }
    public bool RewardGranted { get; set; }
}
