using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Modules;

/// <summary>
/// Blocks access to disabled modules at the service layer (PRS §4.2, §9.3).
/// Read-only: mutating module state is Step 11's ModuleService.
/// Unknown keys fail closed.
/// </summary>
public sealed class ModuleAccessGuard : IModuleAccessGuard
{
    private readonly FitCoreDbContext _db;
    private readonly IModuleRegistry _registry;

    public ModuleAccessGuard(FitCoreDbContext db, IModuleRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<bool> IsEnabledAsync(string moduleKey, CancellationToken ct = default)
    {
        if (_registry.Find(moduleKey) is null) return false;

        var state = await _db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Key == moduleKey, ct);

        return state is { IsEnabled: true };
    }

    public async Task<Result> RequireAsync(string moduleKey, CancellationToken ct = default)
    {
        if (_registry.Find(moduleKey) is null)
            return Result.Fail("module.unknown", $"Unknown module '{moduleKey}'.");

        if (!await IsEnabledAsync(moduleKey, ct))
            return Result.Fail("module.disabled", $"Module '{moduleKey}' is disabled.");

        return Result.Ok();
    }
}
