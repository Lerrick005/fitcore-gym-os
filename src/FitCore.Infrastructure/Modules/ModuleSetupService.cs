using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Modules;
using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Modules;

public sealed class ModuleSetupService : IModuleSetupService
{
    private readonly FitCoreDbContext _db;
    private readonly IModuleRegistry _registry;
    private readonly IClock _clock;

    public ModuleSetupService(FitCoreDbContext db, IModuleRegistry registry, IClock clock)
    {
        _db = db;
        _registry = registry;
        _clock = clock;
    }

    public async Task<Result> EnableModulesAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var requested = (keys ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Validate all requested keys
        foreach (var key in requested)
        {
            if (_registry.Find(key) is null)
                return Result.Fail("module.unknown", $"Unknown module '{key}'.");
        }

        // Compute transitive closure of dependencies
        var closure = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(requested);
        while (queue.Count > 0)
        {
            var key = queue.Dequeue();
            if (!closure.Add(key)) continue;
            var def = _registry.Find(key);
            if (def is null) return Result.Fail("module.unknown", $"Unknown module '{key}'.");
            foreach (var dep in def.Dependencies) queue.Enqueue(dep);
        }

        var now = _clock.UtcNow;
        var existingRows = await _db.Modules
            .Where(m => closure.Contains(m.Key))
            .ToListAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var key in closure)
            {
                var row = existingRows.FirstOrDefault(m => m.Key == key);
                if (row is null)
                {
                    _db.Modules.Add(new ModuleState
                    {
                        Key = key,
                        IsEnabled = true,
                        EnabledAtUtc = now,
                        DisabledAtUtc = null
                    });
                }
                else if (!row.IsEnabled)
                {
                    row.IsEnabled = true;
                    row.EnabledAtUtc = now;
                    row.DisabledAtUtc = null;
                }
                // If already enabled, leave EnabledAtUtc unchanged (idempotent).
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result.Ok();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetEnabledKeysAsync(CancellationToken ct = default)
    {
        var keys = await _db.Modules
            .AsNoTracking()
            .Where(m => m.IsEnabled)
            .OrderBy(m => m.Key)
            .Select(m => m.Key)
            .ToListAsync(ct);
        return keys;
    }

    public async Task<Result> DisableModulesAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var toDisable = new HashSet<string>((keys ?? Array.Empty<string>()).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()), StringComparer.Ordinal);

        // Block if any enabled module depends on one we're disabling
        var allStates = await GetAllAsync(ct);
        if (!allStates.IsSuccess) return Result.Fail(allStates.ErrorCode!, allStates.ErrorMessage!);

        var enabledKeys = allStates.Value!.Where(s => s.IsEnabled).Select(s => s.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var key in toDisable)
        {
            var dependents = _registry.DependentsOf(key)
                .Where(d => enabledKeys.Contains(d.Key) && !toDisable.Contains(d.Key))
                .ToList();
            if (dependents.Count > 0)
                return Result.Fail("module.has_dependents",
                    $"Cannot disable '{key}'. Enabled module(s) depend on it: {string.Join(", ", dependents.Select(d => d.DisplayName))}.");
        }

        var now = _clock.UtcNow;
        var rows = await _db.Modules.Where(m => toDisable.Contains(m.Key)).ToListAsync(ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var row in rows)
            {
                if (row.IsEnabled)
                {
                    row.IsEnabled = false;
                    row.DisabledAtUtc = now;
                }
            }
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result.Ok();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result<IReadOnlyList<ModuleStateDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var enabled = await _db.Modules
            .AsQueryable()
            .Where(m => m.IsEnabled)
            .Select(m => m.Key)
            .ToListAsync(ct);
        var enabledSet = enabled.ToHashSet(StringComparer.Ordinal);

        var list = _registry.All
            .Select(def => new ModuleStateDto(def.Key, def.DisplayName, enabledSet.Contains(def.Key), def.Dependencies))
            .ToList();

        return Result<IReadOnlyList<ModuleStateDto>>.Ok(list);
    }
}



