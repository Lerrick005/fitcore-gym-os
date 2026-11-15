namespace FitCore.Application.Settings;

public sealed record BackupSettings : ISettingsGroup
{
    public static string GroupKey => "backup";

    public bool ScheduledBackupsEnabled { get; init; } = true;
    public int ScheduledIntervalHours { get; init; } = 24;
    public string SecondaryLocationPath { get; init; } = string.Empty;
    public int RetentionCount { get; init; } = 30;
}
