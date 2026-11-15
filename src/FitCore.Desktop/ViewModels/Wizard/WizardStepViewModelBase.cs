using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Common;
using FitCore.Infrastructure.Services;

namespace FitCore.Desktop.ViewModels.Wizard;

/// <summary>
/// Base for wizard step view models (PRS §10).
/// Handles: busy state, validation message surfacing, save-in-system-context, error capture.
/// Each step overrides LoadAsyncCore / ValidateAsyncCore / SaveAsyncCore.
/// </summary>
public abstract partial class WizardStepViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;

    public abstract string Title { get; }
    public abstract string Description { get; }

    /// <summary>Load persisted state into this VM. Called when the step is shown.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try { await LoadAsyncCore(ct); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    /// <summary>Validate current state. Returns null on success, error summary on failure.</summary>
    public async Task<string?> ValidateAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var err = await ValidateAsyncCore(ct);
            if (err is not null) ErrorMessage = err;
            return err;
        }
        catch (Exception ex) { ErrorMessage = ex.Message; return ex.Message; }
        finally { IsBusy = false; }
    }

    /// <summary>Persist current state inside the wizard's system context (PRS §10, bypass of permission).</summary>
    public async Task<Result> SaveAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            using (SettingsSystemContext.Enter())
            {
                var result = await SaveAsyncCore(ct);
                if (!result.IsSuccess)
                    ErrorMessage = result.ErrorMessage ?? "Save failed.";
                return result;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Result.Fail("wizard.save.exception", ex.Message);
        }
        finally { IsBusy = false; }
    }

    protected abstract Task LoadAsyncCore(CancellationToken ct);
    protected abstract Task<string?> ValidateAsyncCore(CancellationToken ct);
    protected abstract Task<Result> SaveAsyncCore(CancellationToken ct);

    /// <summary>Helper: joins multiple Result errors into one message.</summary>
    protected static string JoinErrors(IEnumerable<string?> messages)
    {
        var list = new List<string>();
        foreach (var m in messages) if (!string.IsNullOrWhiteSpace(m)) list.Add(m!);
        return string.Join(" ", list);
    }
}
