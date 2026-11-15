using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Importing;

namespace FitCore.Desktop.ViewModels.Members;

public sealed class ImportRowVm
{
    public int RowNumber { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
}

public sealed partial class MemberImportViewModel : ObservableObject
{
    private readonly IMemberImportService _import;
    private string? _selectedFilePath;
    private MemberImportPreview? _lastPreview;

    [ObservableProperty] private ObservableCollection<ImportRowVm> _rows = new();
    [ObservableProperty] private string _fileName = "(no file selected)";
    [ObservableProperty] private int _totalRows;
    [ObservableProperty] private int _readyCount;
    [ObservableProperty] private int _duplicateCount;
    [ObservableProperty] private int _invalidCount;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canCommit;

    public event EventHandler<bool>? CloseRequested;

    public MemberImportViewModel(IMemberImportService import) => _import = import;

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Select member CSV"
        };
        if (dlg.ShowDialog() != true) return;

        _selectedFilePath = dlg.FileName;
        FileName = System.IO.Path.GetFileName(dlg.FileName);
        await PreviewAsync();
    }

    private async Task PreviewAsync()
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        IsBusy = true;
        StatusMessage = "Validating...";
        CanCommit = false;
        try
        {
            var result = await _import.PreviewAsync(_selectedFilePath);
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Preview failed.";
                Rows.Clear();
                return;
            }
            ApplyPreview(result.Value!);
            StatusMessage = $"Preview: {result.Value!.ReadyCount} ready, {result.Value.DuplicateCount} duplicate, {result.Value.InvalidCount} invalid.";
            CanCommit = result.Value.ReadyCount > 0;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CommitAsync()
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        IsBusy = true;
        StatusMessage = "Importing...";
        try
        {
            var result = await _import.CommitAsync(_selectedFilePath);
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Import failed.";
                return;
            }
            ApplyPreview(result.Value!);
            StatusMessage = $"Imported {result.Value!.ReadyCount} members.";
            CanCommit = false;
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    [RelayCommand]
    private void DownloadTemplate()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "members-template.csv",
            Title = "Save member template"
        };
        if (dlg.ShowDialog() != true) return;

        System.IO.File.WriteAllText(dlg.FileName,
            "FullName,Phone,Email,DateOfBirth,Gender,Notes\n" +
            "John Kimani,0712345678,john@example.com,1990-05-15,Male,\n" +
            "Mary Wanjiku,0722000001,,1992-08-22,Female,Prefers morning sessions\n");
        StatusMessage = "Template saved.";
    }

    private void ApplyPreview(MemberImportPreview preview)
    {
        _lastPreview = preview;
        TotalRows = preview.TotalRows;
        ReadyCount = preview.ReadyCount;
        DuplicateCount = preview.DuplicateCount;
        InvalidCount = preview.InvalidCount;
        Rows.Clear();
        foreach (var r in preview.RowResults)
        {
            Rows.Add(new ImportRowVm
            {
                RowNumber = r.RowNumber,
                FullName = r.FullName,
                Phone = r.Phone,
                Status = r.Status,
                Message = r.Message
            });
        }
    }
}
