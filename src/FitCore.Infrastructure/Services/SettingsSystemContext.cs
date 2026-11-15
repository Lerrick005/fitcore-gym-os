namespace FitCore.Infrastructure.Services;

/// <summary>
/// Scope during which the Settings Service may write settings without an authenticated user.
/// Used ONLY by the first-run wizard (PRS §10: wizard runs before any user exists).
/// Async-safe; cannot leak beyond the using block.
/// </summary>
public static class SettingsSystemContext
{
    private static readonly AsyncLocal<bool> _active = new();

    public static bool IsActive => _active.Value;

    public static IDisposable Enter()
    {
        _active.Value = true;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => _active.Value = false;
    }
}
