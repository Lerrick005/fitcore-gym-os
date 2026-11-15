using FitCore.Application.Settings;

namespace FitCore.Application.Abstractions;

/// <summary>Single path to configuration (PRS §6). Cached, audited, change-event aware.</summary>
public interface ISettingsService
{
    /// <summary>Returns the typed settings group, cached. Creates defaults on first access.</summary>
    Task<T> GetAsync<T>(CancellationToken ct = default) where T : class, ISettingsGroup, new();

    /// <summary>Validates, persists, audits, invalidates cache, raises Changed.</summary>
    Task<Common.Result> SaveAsync<T>(T value, CancellationToken ct = default)
        where T : class, ISettingsGroup, new();

    /// <summary>Raised after a successful save. Subscribers refresh.</summary>
    event EventHandler<SettingsChangedEventArgs>? Changed;
}
