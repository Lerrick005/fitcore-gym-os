using FitCore.Application.Common;

namespace FitCore.Application.Licensing;

public interface ILicenceService
{
    /// <summary>Current licence status. Never throws.</summary>
    Task<LicenceStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Load and verify a licence file. Safe on tampered/invalid files — returns Tampered state.</summary>
    Task<Result<LicenceStatus>> LoadLicenceAsync(string filePath, CancellationToken ct = default);

    /// <summary>Whether a specific module is entitled by the current licence.</summary>
    Task<bool> IsModuleEntitledAsync(string moduleKey, CancellationToken ct = default);

    /// <summary>Whether the "Powered by FitCore" attribution should be visible.</summary>
    Task<bool> ShouldShowAttributionAsync(CancellationToken ct = default);

    /// <summary>Custom attribution text (from licence) or default.</summary>
    Task<string> GetAttributionTextAsync(CancellationToken ct = default);
}

