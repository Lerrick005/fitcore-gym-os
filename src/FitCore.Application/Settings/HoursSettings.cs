namespace FitCore.Application.Settings;

public sealed record DayHours
{
    public bool IsClosed { get; init; }
    public string OpenTime { get; init; } = "06:00";
    public string CloseTime { get; init; } = "22:00";
    public string? SplitOpenTime { get; init; }
    public string? SplitCloseTime { get; init; }
}

public sealed record HoursSettings : ISettingsGroup
{
    public static string GroupKey => "hours";

    public DayHours Monday { get; init; } = new();
    public DayHours Tuesday { get; init; } = new();
    public DayHours Wednesday { get; init; } = new();
    public DayHours Thursday { get; init; } = new();
    public DayHours Friday { get; init; } = new();
    public DayHours Saturday { get; init; } = new();
    public DayHours Sunday { get; init; } = new();

    public bool WarnOutsideHours { get; init; } = true;
    public bool BlockOutsideHours { get; init; }
}
