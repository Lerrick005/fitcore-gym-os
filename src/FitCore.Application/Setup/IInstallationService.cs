using FitCore.Application.Common;
using InstallationEntity = FitCore.Domain.Entities.Installation;

namespace FitCore.Application.Setup;

/// <summary>
/// Installation row lifecycle: create on first run, track wizard progress, complete setup
/// (PRS §5, §10, §13.1).
/// </summary>
public interface IInstallationService
{
    /// <summary>Idempotent: returns the existing Installation row, or creates one with a new GUID.</summary>
    Task<InstallationEntity> GetOrCreateAsync(CancellationToken ct = default);

    /// <summary>Returns the Installation row; throws if it does not exist (programmer error).</summary>
    Task<InstallationEntity> GetAsync(CancellationToken ct = default);

    /// <summary>Persists the wizard's current step (0..10).</summary>
    Task<Result> SetStepAsync(int step, CancellationToken ct = default);

    /// <summary>Marks setup complete (SetupCompleted = true, SetupStep = 10).</summary>
    Task<Result> CompleteSetupAsync(CancellationToken ct = default);

    /// <summary>Convenience: true once setup is complete.</summary>
    Task<bool> IsSetupCompletedAsync(CancellationToken ct = default);
}
