using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Backup;
using FitCore.Application.Common;
using FitCore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Infrastructure.Backup;

public sealed class BackupService : IBackupService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _dbPath;
    private readonly string _backupFolder;

    public string BackupFolder => _backupFolder;

    public BackupService(IServiceScopeFactory scopeFactory, string dbPath)
    {
        _scopeFactory = scopeFactory;
        _dbPath = dbPath;
        _backupFolder = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "backups");
        Directory.CreateDirectory(_backupFolder);
    }

    public async Task<Result<BackupInfo>> CreateBackupAsync(CancellationToken ct = default)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fileName = $"fitcore-backup-{timestamp}.db";
            var destPath = Path.Combine(_backupFolder, fileName);

            // Use SQLite online backup API — safe while app is running
            await using var source = new SqliteConnection($"Data Source={_dbPath}");
            await source.OpenAsync(ct);
            await using var dest = new SqliteConnection($"Data Source={destPath}");
            await dest.OpenAsync(ct);
            source.BackupDatabase(dest);

            var info = new BackupInfo(fileName, destPath, new FileInfo(destPath).Length, DateTime.UtcNow);
            return Result<BackupInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<BackupInfo>.Fail("backup.failed", $"Backup failed: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_backupFolder))
                return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Ok(new List<BackupInfo>()));

            var files = Directory.GetFiles(_backupFolder, "fitcore-backup-*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.CreationTimeUtc)
                .Select(fi => new BackupInfo(fi.Name, fi.FullName, fi.Length, fi.CreationTimeUtc))
                .ToList();

            return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Ok((IReadOnlyList<BackupInfo>)files));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Fail("backup.list.failed", ex.Message));
        }
    }

    public async Task<Result> RestoreAsync(string backupPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
            return Result.Fail("backup.not_found", "Backup file does not exist.");

        try
        {
            // 1. Create safety backup of current state
            var safetyResult = await CreateBackupAsync(ct);
            if (!safetyResult.IsSuccess)
                return Result.Fail("backup.safety.failed", "Could not create safety backup before restore.");

            // 2. Close current connections, replace DB
            SqliteConnection.ClearAllPools();

            // 3. Copy backup over current DB
            File.Copy(backupPath, _dbPath, overwrite: true);

            // 4. Delete WAL/SHM so fresh state
            var wal = _dbPath + "-wal";
            var shm = _dbPath + "-shm";
            if (File.Exists(wal)) File.Delete(wal);
            if (File.Exists(shm)) File.Delete(shm);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("backup.restore.failed", $"Restore failed: {ex.Message}");
        }
    }
}
