namespace FitCore.Application.Settings;

public sealed record PlanTemplate
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long PriceMinor { get; init; }
    public int DurationValue { get; init; }
    public int DurationUnit { get; init; } // FitCore.Domain.Enums.DurationUnit as int
    public int? FreezeAllowanceDays { get; init; }
    public int? VisitLimit { get; init; }
    public long? JoiningFeeMinor { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
}

public sealed record PlansSettings : ISettingsGroup
{
    public static string GroupKey => "plans";

    public List<PlanTemplate> Templates { get; init; } = new();
}
