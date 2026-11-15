namespace FitCore.Application.Settings;

public sealed record DiscountsSettings : ISettingsGroup
{
    public static string GroupKey => "discounts";

    public bool AllowStacking { get; init; }
    public int MaxStackedDiscounts { get; init; } = 1;
    public List<string> ActivePromoCodes { get; init; } = new();
}
