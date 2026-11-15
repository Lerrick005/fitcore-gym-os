using FitCore.Application.Common;

namespace FitCore.Application.Modules;

public interface IModuleSetupService
{
    Task<Result> EnableModulesAsync(IEnumerable<string> keys, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetEnabledKeysAsync(CancellationToken ct = default);

    Task<Result> DisableModulesAsync(IEnumerable<string> keys, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ModuleStateDto>>> GetAllAsync(CancellationToken ct = default);
}
