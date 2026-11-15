namespace FitCore.Application.Settings;

public sealed record RulesSettings : ISettingsGroup
{
    public static string GroupKey => "rules";

    public int GracePeriodDays { get; init; }
    public string LateRenewalPolicy { get; init; } = "none";
    public int MaxFreezeDays { get; init; }
    public long FreezeFeeMinor { get; init; }
    public string RenewalDatePolicy { get; init; } = "from_expiry";
    public string RefundPolicy { get; init; } = "no_refund";
    public bool PartPaymentsAllowed { get; init; } = true;
    public bool ExpiredMembersCanCheckIn { get; init; }
}
