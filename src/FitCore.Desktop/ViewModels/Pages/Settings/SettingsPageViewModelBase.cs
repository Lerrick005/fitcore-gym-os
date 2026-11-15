using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Common;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

/// <summary>Base for individual settings group pages (Branding, Business, etc.).</summary>
public abstract partial class SettingsPageViewModelBase : PageViewModelBase
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasUnsavedChanges;

    public abstract string GroupName { get; }
    public abstract string GroupDescription { get; }

    public override string Title => GroupName;

    public async Task<Result> SaveAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var result = await SaveCoreAsync(ct);
            StatusMessage = result.IsSuccess
                ? "Saved."
                : $"Error: {result.ErrorMessage}";
            if (result.IsSuccess) HasUnsavedChanges = false;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected abstract Task<Result> SaveCoreAsync(CancellationToken ct);
}
