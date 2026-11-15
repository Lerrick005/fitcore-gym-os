namespace FitCore.Application.Settings;

public sealed record BusinessSettings : ISettingsGroup
{
    public static string GroupKey => "business";

    public string PhonePrimary { get; init; } = string.Empty;
    public string PhoneSecondary { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
    public string PhysicalAddress { get; init; } = string.Empty;
    public string RegistrationNumber { get; init; } = string.Empty;
    public string TaxNumber { get; init; } = string.Empty;
    public string SocialHandles { get; init; } = string.Empty;
}
