namespace FitCore.Application.Session;

/// <summary>Auto-lock after inactivity. Fires when configured timeout elapses with no user activity.</summary>
public interface IAutoLockService
{
    void StartTracking();
    void StopTracking();
    void NotifyActivity();
    bool IsEnabled { get; }
    int TimeoutMinutes { get; }
    event EventHandler? LockRequested;
}
