using System.Threading;

namespace FitCore.Infrastructure.Hosting;

public sealed class SingleInstanceGuard : IDisposable
{
    // TODO(step-3): switch to a per-Installation-ID mutex name once Installation table exists.
    private const string MutexName = @"Global\FitCore.SingleInstance";

    private readonly Mutex _mutex;
    private readonly bool _isFirstInstance;
    private bool _disposed;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: false, name: MutexName, out _isFirstInstance);
    }

    /// <summary>True if this process created the mutex (i.e. it is the only instance).</summary>
    public bool TryAcquire() => _isFirstInstance;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mutex.Dispose();
    }
}
