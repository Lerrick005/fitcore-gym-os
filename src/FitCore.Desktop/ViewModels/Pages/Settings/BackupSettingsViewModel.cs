using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Backup;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed class BackupRow
{
    public string FileName { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string SizeDisplay { get; init; } = string.Empty;
    public string CreatedDisplay { get; init; } = string.Empty;
}

public sealed partial class BackupSettingsViewModel : SettingsPageViewModelBase
{
    private readonly IBackupService _backup;

    [ObservableProperty] private ObservableCollection<BackupRow> _rows = new();
    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public override string GroupName => "Backup & restore";
    public override string GroupDescription => "Protect your data. Backups save the whole database.";

    public event EventHandler<string>? RestoreRequested;

    public BackupSettingsViewModel(IBackupService backup) => _backup = backup;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        BackupFolder = _backup.BackupFolder;
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _backup.ListBackupsAsync(ct);
            Rows.Clear();
            if (!result.IsSuccess) { StatusMessage = result.ErrorMessage; return; }
            foreach (var b in result.Value!)
            {
                Rows.Add(new BackupRow
                {
                    FileName = b.FileName,
                    FullPath = b.FullPath,
                    SizeDisplay = FormatSize(b.SizeBytes),
                    CreatedDisplay = b.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                });
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var r = await _backup.CreateBackupAsync();
            StatusMessage = r.IsSuccess ? $"Backup created: {r.Value!.FileName}" : r.ErrorMessage;
            await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void RestoreBackup(BackupRow? row)
    {
        if (row is null) return;
        RestoreRequested?.Invoke(this, row.FullPath);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024):F1} MB";
    }

    protected override Task<FitCore.Application.Common.Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
