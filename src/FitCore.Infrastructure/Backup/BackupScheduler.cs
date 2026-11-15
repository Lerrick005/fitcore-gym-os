using System;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Backup;
using FitCore.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FitCore.Infrastructure.Backup;

/// <summary>
/// Runs backups periodically in the background. Checks BackupSettings.ScheduledBackupsEnabled,
/// ScheduledIntervalHours, and RetentionCount. Runs on the host lifecycle.
/// </summary>
public sealed class BackupScheduler : IBackupScheduler, IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _log = Serilog.Log.ForContext<BackupScheduler>();
    private Timer? _timer;
    private bool _running;

    public DateTime? LastRunUtc { get; private set; }

    public BackupScheduler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        // Check every 30 minutes whether it's time to back up
        _timer = new Timer(CheckAsync, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(30));
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _running = false;
    }

    public Task StartAsync(CancellationToken cancellationToken) { Start(); return Task.CompletedTask; }
    public Task StopAsync(CancellationToken cancellationToken) { Stop(); return Task.CompletedTask; }

    private async void CheckAsync(object? state)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = await scope.ServiceProvider.GetRequiredService<ISettingsService>().GetAsync<BackupSettings>();
            var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();

            if (!settings.ScheduledBackupsEnabled) return;

            var list = await backup.ListBackupsAsync();
            if (!list.IsSuccess) return;

            var last = list.Value!.OrderByDescending(b => b.CreatedAtUtc).FirstOrDefault();
            var nextDue = last is null ? DateTime.UtcNow : last.CreatedAtUtc.AddHours(settings.ScheduledIntervalHours);

            if (DateTime.UtcNow >= nextDue)
            {
                var r = await backup.CreateBackupAsync();
                if (r.IsSuccess)
                {
                    LastRunUtc = DateTime.UtcNow;
                    _log.Information("Scheduled backup created: {File}", r.Value!.FileName);

                    // Retention: delete older backups beyond configured count
                    var afterCreate = await backup.ListBackupsAsync();
                    if (afterCreate.IsSuccess && afterCreate.Value!.Count > settings.RetentionCount)
                    {
                        var toDelete = afterCreate.Value!
                            .OrderByDescending(b => b.CreatedAtUtc)
                            .Skip(settings.RetentionCount)
                            .ToList();
                        foreach (var old in toDelete)
                        {
                            try { System.IO.File.Delete(old.FullPath); _log.Information("Deleted old backup: {File}", old.FileName); }
                            catch (Exception ex) { _log.Warning(ex, "Could not delete old backup {File}", old.FileName); }
                        }
                    }
                }
                else
                {
                    _log.Warning("Scheduled backup failed: {Code} {Message}", r.ErrorCode, r.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "BackupScheduler tick failed");
        }
    }

    public void Dispose() => _timer?.Dispose();
}

