namespace FitCore.Application.Settings;

/// <summary>
/// Gym type and operational preferences (PRS §8.7).
/// Wizard-only choices that persist here; never restrict later changes.
/// </summary>
public sealed record GymPreferencesSettings : ISettingsGroup
{
    public static string GroupKey => "gymPreferences";

    public string GymType { get; init; } = string.Empty; // general, crossfit, women-only, boxing-mma, yoga-studio, powerlifting, multi-facility
    public string CheckInMethod { get; init; } = "manual_search"; // manual_search, member_number, qr_code
    public bool DayPassSupported { get; init; }
    public bool LockerManagementEnabled { get; init; }
    public bool ClassBookingEnabled { get; init; }
    public bool PhotoRequiredOnRegistration { get; init; }
    public List<string> RequiredMemberFields { get; init; } = new(); // e.g. ["FullName", "Phone"]
}
