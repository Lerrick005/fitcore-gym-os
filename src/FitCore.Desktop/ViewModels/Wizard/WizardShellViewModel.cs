using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Setup;
using FitCore.Application.Abstractions;
using FitCore.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.ViewModels.Wizard;

/// <summary>
/// Orchestrates the 10-step first-run wizard (PRS §10).
/// Resolves step VMs from DI per step, saves after each Next, resumes on startup.
/// </summary>
public sealed partial class WizardShellViewModel : ObservableObject
{
    private static readonly Type[] StepTypes =
    {
        typeof(BusinessIdentityViewModel),
        typeof(RolesAndOwnerViewModel),
        typeof(ReviewViewModel)
    };

    private readonly IServiceProvider _sp;
    private readonly IInstallationService _installation;

    [ObservableProperty] private int _currentStepIndex;
    [ObservableProperty] private WizardStepViewModelBase? _currentStepViewModel;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private bool _isOnLastStep;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoNext;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler? SetupCompleted;

    public WizardShellViewModel(IServiceProvider sp, IInstallationService installation)
    {
        _sp = sp;
        _installation = installation;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            await _installation.GetOrCreateAsync(ct);
            var inst = await _installation.GetAsync(ct);

            if (inst.SetupCompleted)
            {
                SetupCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            var step = inst.SetupStep;
            if (step < 1 || step > 3) step = 1;
            CurrentStepIndex = step - 1;
            await LoadCurrentStepAsync(ct);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCurrentStepAsync(CancellationToken ct = default)
    {
        var vm = (WizardStepViewModelBase)_sp.GetRequiredService(StepTypes[CurrentStepIndex]);
        await vm.LoadAsync(ct);
        CurrentStepViewModel = vm;

        ProgressPercent = ((CurrentStepIndex + 1) / (double)StepTypes.Length) * 100.0;
        IsOnLastStep = CurrentStepIndex == StepTypes.Length - 1;
        CanGoBack = CurrentStepIndex > 0;
        CanGoNext = !IsOnLastStep;
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (CurrentStepViewModel is null) return;
        IsBusy = true;
        try
        {
            var v = await CurrentStepViewModel.ValidateAsync();
            if (v is not null) return;

            var save = await CurrentStepViewModel.SaveAsync();
            if (!save.IsSuccess) return;

            var nextIndex = CurrentStepIndex + 1;
            if (nextIndex >= StepTypes.Length)
            {
                // Finish
                await _installation.CompleteSetupAsync();
                SetupCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            await _installation.SetStepAsync(nextIndex + 1);
            CurrentStepIndex = nextIndex;
            await LoadCurrentStepAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (CurrentStepIndex <= 0) return;
        IsBusy = true;
        try
        {
            await _installation.SetStepAsync(CurrentStepIndex);
            CurrentStepIndex -= 1;
            await LoadCurrentStepAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FinishAsync() => await NextAsync();
}


