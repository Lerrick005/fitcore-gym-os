using FitCore.Application.Common;

namespace FitCore.Application.Backup;

public sealed record BackupInfo(
    string FileName,
    string FullPath,
    long SizeBytes,
    DateTime CreatedAtUtc);

public interface IBackupService
{
    /// <summary>Create a manual backup. Returns the file path on success.</summary>
    Task<Result<BackupInfo>> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>List existing backups in the backup folder, newest first.</summary>
    Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(CancellationToken ct = default);

    /// <summary>Restore from a backup file. Creates a safety copy first.</summary>
    Task<Result> RestoreAsync(string backupPath, CancellationToken ct = default);

    /// <summary>Get the backup folder path.</summary>
    string BackupFolder { get; }
}
