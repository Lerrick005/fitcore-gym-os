using FitCore.Application.Settings;

namespace FitCore.Application.Settings;

/// <summary>
/// Loads vendor-supplied defaults from defaults/fitcore.defaults.json (PRS §0 rule 3, §10).
/// These are wizard suggestions only — the owner can change everything.
/// </summary>
public interface IDefaultsProvider
{
    Task<BrandingSettings> GetBrandingDefaultsAsync(CancellationToken ct = default);
    Task<BusinessSettings> GetBusinessDefaultsAsync(CancellationToken ct = default);
    Task<LocaleSettings> GetLocaleDefaultsAsync(CancellationToken ct = default);
    Task<HoursSettings> GetHoursDefaultsAsync(CancellationToken ct = default);
    Task<PlansSettings> GetPlansDefaultsAsync(CancellationToken ct = default);
    Task<ReceiptSettings> GetReceiptDefaultsAsync(CancellationToken ct = default);
    Task<GymPreferencesSettings> GetGymPreferencesDefaultsAsync(CancellationToken ct = default);
}
