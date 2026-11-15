using System;
using System.Reflection;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Plans;
using FitCore.Application.Settings;
using FitCore.Application.Setup;
using FitCore.Infrastructure.Persistence;
using InstallationEntity = FitCore.Domain.Entities.Installation;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Setup;

public sealed class InstallationService : IInstallationService
{
    private readonly FitCoreDbContext _db;
    private readonly IClock _clock;
    private readonly IMembershipPlanService _plans;
    private readonly ISettingsService _settings;
    private readonly IDefaultsProvider _defaults;

    public InstallationService(
        FitCoreDbContext db,
        IClock clock,
        IMembershipPlanService plans,
        ISettingsService settings,
        IDefaultsProvider defaults)
    {
        _db = db;
        _clock = clock;
        _plans = plans;
        _settings = settings;
        _defaults = defaults;
    }

    public async Task<InstallationEntity> GetOrCreateAsync(CancellationToken ct = default)
    {
        var existing = await _db.Installations.FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var now = _clock.UtcNow;
        var installation = new InstallationEntity
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = now,
            SchemaVersion = 1,
            AppVersionAtCreation = ResolveAppVersion(),
            SetupCompleted = false,
            SetupStep = (int)WizardStep.Fresh
        };
        _db.Installations.Add(installation);
        await _db.SaveChangesAsync(ct);

        // Seed Kenya/vendor defaults NOW — so any screen showing locale before wizard
        // finish (e.g. the Confirm step) sees KES / Africa/Nairobi, not USD / UTC.
        try { await ApplyDefaultsAsync(ct); }
        catch { /* non-fatal */ }

        return installation;
    }

    public async Task<InstallationEntity> GetAsync(CancellationToken ct = default)
    {
        var installation = await _db.Installations.FirstOrDefaultAsync(ct);
        if (installation is null)
            throw new InvalidOperationException("Installation row does not exist. Call GetOrCreateAsync first.");
        return installation;
    }

    public async Task<Result> SetStepAsync(int step, CancellationToken ct = default)
    {
        if (step < (int)WizardStep.Fresh || step > (int)WizardStep.Confirm)
            return Result.Fail("installation.step.invalid", $"Step {step} is out of range 0..3.");

        var installation = await _db.Installations.FirstOrDefaultAsync(ct);
        if (installation is null)
            return Result.Fail("installation.missing", "Installation row does not exist.");

        installation.SetupStep = step;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CompleteSetupAsync(CancellationToken ct = default)
    {
        var installation = await _db.Installations.FirstOrDefaultAsync(ct);
        if (installation is null)
            return Result.Fail("installation.missing", "Installation row does not exist.");

        // Re-apply any missing defaults (idempotent — only fills empty groups)
        await ApplyDefaultsAsync(ct);

        installation.SetupCompleted = true;
        installation.SetupStep = (int)WizardStep.Confirm;
        await _db.SaveChangesAsync(ct);

        // Seed plans
        try { await _plans.GetActiveAsync(ct); }
        catch { /* non-fatal */ }

        return Result.Ok();
    }

    public async Task<bool> IsSetupCompletedAsync(CancellationToken ct = default)
    {
        return await _db.Installations.AsQueryable().AnyAsync(x => x.SetupCompleted, ct);
    }

    private async Task ApplyDefaultsAsync(CancellationToken ct)
    {
        // Locale — apply Kenya defaults if still at model defaults
        var currentLocale = await _settings.GetAsync<LocaleSettings>(ct);
        if (currentLocale.CurrencyCode == "USD" && currentLocale.TimeZoneId == "UTC")
        {
            var localeDefaults = await _defaults.GetLocaleDefaultsAsync(ct);
            await _settings.SaveAsync(localeDefaults, ct);
        }

        // Hours — apply Kenya/JSON defaults if still at model default
        var currentHours = await _settings.GetAsync<HoursSettings>(ct);
        if (currentHours.Monday.OpenTime == "06:00"
            && currentHours.Sunday.CloseTime == "18:00")
        {
            // HoursSettings model default is 06:00-22:00 every day.
            // JSON Kenya default has Sun 08:00-18:00 — detect via Sunday.CloseTime
            var hoursDefaults = await _defaults.GetHoursDefaultsAsync(ct);
            await _settings.SaveAsync(hoursDefaults, ct);
        }

        // GymPreferences
        var currentGymPref = await _settings.GetAsync<GymPreferencesSettings>(ct);
        if (string.IsNullOrWhiteSpace(currentGymPref.GymType) || currentGymPref.GymType == "general")
        {
            var gymPrefDefaults = await _defaults.GetGymPreferencesDefaultsAsync(ct);
            await _settings.SaveAsync(gymPrefDefaults, ct);
        }

        // Receipt
        var currentReceipt = await _settings.GetAsync<ReceiptSettings>(ct);
        if (string.IsNullOrWhiteSpace(currentReceipt.FooterText))
        {
            var receiptDefaults = await _defaults.GetReceiptDefaultsAsync(ct);
            await _settings.SaveAsync(receiptDefaults, ct);
        }

        // Branding — colors if still at model default #3B82F6
        var currentBranding = await _settings.GetAsync<BrandingSettings>(ct);
        if (currentBranding.PrimaryColor == "#3B82F6")
        {
            var brandingDefaults = await _defaults.GetBrandingDefaultsAsync(ct);
            var merged = brandingDefaults with
            {
                BusinessName = currentBranding.BusinessName,
                TradingName = currentBranding.TradingName,
                Tagline = currentBranding.Tagline
            };
            await _settings.SaveAsync(merged, ct);
        }
    }

    private static string ResolveAppVersion()
    {
        var entry = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        if (!string.IsNullOrEmpty(entry)) return entry!;
        var executing = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        return executing ?? "unknown";
    }
}
