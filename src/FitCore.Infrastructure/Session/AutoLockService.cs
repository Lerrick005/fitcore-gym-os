using System;
using System.Threading;
using FitCore.Application.Abstractions;
using FitCore.Application.Session;
using FitCore.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FitCore.Infrastructure.Session;

public sealed class AutoLockService : IAutoLockService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private System.Threading.Timer? _timer;
    private DateTime _lastActivityUtc;

    public bool IsEnabled { get; private set; }
    public int TimeoutMinutes { get; private set; } = 15;
    public event EventHandler? LockRequested;

    public AutoLockService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void StartTracking()
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var ui = settings.GetAsync<UiSettings>().GetAwaiter().GetResult();
        TimeoutMinutes = ui.AutoLockMinutes > 0 ? ui.AutoLockMinutes : 15;
        IsEnabled = TimeoutMinutes > 0;
        _lastActivityUtc = DateTime.UtcNow;
        _timer = new System.Threading.Timer(Check, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void StopTracking()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void NotifyActivity() => _lastActivityUtc = DateTime.UtcNow;

    private void Check(object? state)
    {
        if (!IsEnabled) return;
        if ((DateTime.UtcNow - _lastActivityUtc).TotalMinutes >= TimeoutMinutes)
        {
            _lastActivityUtc = DateTime.UtcNow;
            Log.Information("Auto-lock firing after {Min} minutes inactivity", TimeoutMinutes);
            LockRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => _timer?.Dispose();
}

