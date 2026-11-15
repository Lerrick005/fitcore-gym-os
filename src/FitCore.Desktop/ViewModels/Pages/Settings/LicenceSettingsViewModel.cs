using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Licensing;
using FitCore.Desktop.ViewModels.Pages.Settings;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class LicenceSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ILicenceService _licence;

    [ObservableProperty] private string _state = "Missing";
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _editionLabel = string.Empty;
    [ObservableProperty] private string _installationId = string.Empty;
    [ObservableProperty] private string _expiresDisplay = "—";
    [ObservableProperty] private string _modulesDisplay = "—";
    [ObservableProperty] private string? _statusMessageLocal;

    public override string GroupName => "Licence";
    public override string GroupDescription => "Your FitCore licence. Managed by your vendor.";

    public LicenceSettingsViewModel(ILicenceService licence) => _licence = licence;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var status = await _licence.GetStatusAsync(ct);
        State = status.State.ToString();
        Message = status.Message;

        if (status.Payload is not null)
        {
            CustomerName = status.Payload.CustomerName;
            EditionLabel = status.Payload.EditionLabel;
            InstallationId = status.Payload.InstallationId;
            ExpiresDisplay = status.Payload.ExpiresAtUtc?.ToString("dd/MM/yyyy") ?? "Never";
            ModulesDisplay = status.Payload.EntitledModules.Count == 0
                ? "(none)"
                : string.Join(", ", status.Payload.EntitledModules);
        }
        else
        {
            CustomerName = "—";
            EditionLabel = "—";
            InstallationId = "—";
            ExpiresDisplay = "—";
            ModulesDisplay = "—";
        }
    }

    [RelayCommand]
    private async Task LoadLicenceFileAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Licence files (*.licence.json)|*.licence.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Select licence file"
        };
        if (dlg.ShowDialog() != true) return;

        var result = await _licence.LoadLicenceAsync(dlg.FileName);
        if (!result.IsSuccess)
        {
            StatusMessageLocal = $"Failed: {result.ErrorMessage}";
            return;
        }

        await LoadAsync();
        StatusMessageLocal = $"Licence loaded: {result.Value!.State}";
    }

    protected override Task<FitCore.Application.Common.Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(FitCore.Application.Common.Result.Ok());
}
