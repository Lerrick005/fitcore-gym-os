namespace FitCore.Application.Abstractions;

/// <summary>Blocks access to disabled modules at the service layer (PRS §4.2, §9.3).</summary>
public interface IModuleAccessGuard
{
    Task<bool> IsEnabledAsync(string moduleKey, CancellationToken ct = default);
    Task<Common.Result> RequireAsync(string moduleKey, CancellationToken ct = default);
}
