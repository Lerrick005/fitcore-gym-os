namespace FitCore.Application.Backup;

/// <summary>Background service that runs scheduled backups based on BackupSettings.</summary>
public interface IBackupScheduler
{
    void Start();
    void Stop();
    DateTime? LastRunUtc { get; }
}
