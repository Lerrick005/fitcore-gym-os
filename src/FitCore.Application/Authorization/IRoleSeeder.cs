using FitCore.Application.Common;

namespace FitCore.Application.Authorization;

/// <summary>Seeds the permission catalogue and default roles. Idempotent.</summary>
public interface IRoleSeeder
{
    /// <summary>Ensures all permissions exist and default roles are created (once).</summary>
    Task<Result> SeedAsync(CancellationToken ct = default);
}
