namespace FitCore.Application.Settings;

public sealed record ThresholdsSettings : ISettingsGroup
{
    public static string GroupKey => "thresholds";

    public int InactivityDaysForAtRisk { get; init; } = 14;
    public int DaysBeforeExpiryForReminder { get; init; } = 7;
    public int VisitsPerWeekForActive { get; init; } = 2;
    public int FollowUpCadenceDays { get; init; } = 7;
    public int BackupStalenessWarningDays { get; init; } = 7;
}
