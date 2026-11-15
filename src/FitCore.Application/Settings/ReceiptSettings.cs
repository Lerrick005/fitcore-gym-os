namespace FitCore.Application.Settings;

public sealed record ReceiptSettings : ISettingsGroup
{
    public static string GroupKey => "receipt";

    public string HeaderText { get; init; } = string.Empty;
    public string FooterText { get; init; } = string.Empty;
    public string PaperType { get; init; } = "thermal80";
    public bool ShowTaxLine { get; init; }
    public string ReceiptNumberPrefix { get; init; } = "R-";
}
