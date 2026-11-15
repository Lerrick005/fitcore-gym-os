using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Common;
using FitCore.Application.Modules;
using FitCore.Desktop.ViewModels.Pages.Settings;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class ModuleToggleVm : ObservableObject
{
    private bool _isEnabled;
    private bool _isChanging;

    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DependenciesText { get; init; } = string.Empty;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                ToggleCallback?.Invoke(this);
        }
    }

    public bool IsChanging
    {
        get => _isChanging;
        set => SetProperty(ref _isChanging, value);
    }

    public Action<ModuleToggleVm>? ToggleCallback { get; set; }}

public sealed partial class ModulesSettingsViewModel : SettingsPageViewModelBase
{
    private readonly IModuleSetupService _modules;

    public ObservableCollection<ModuleToggleVm> Modules { get; } = new();

    public override string GroupName => "Modules";
    public override string GroupDescription => "Optional features. Toggle on to enable, off to hide.";

    public ModulesSettingsViewModel(IModuleSetupService modules) => _modules = modules;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var r = await _modules.GetAllAsync(ct);
        Modules.Clear();
        if (!r.IsSuccess) { StatusMessage = r.ErrorMessage; return; }
        foreach (var m in r.Value!)
        {
            var vm = new ModuleToggleVm
            {
                Key = m.Key,
                DisplayName = m.DisplayName,
                DependenciesText = m.Dependencies.Count == 0 ? "No dependencies" : "Requires: " + string.Join(", ", m.Dependencies),
                IsEnabled = m.IsEnabled
            };
            vm.ToggleCallback = OnToggle;
            Modules.Add(vm);
        }
    }

    private async void OnToggle(ModuleToggleVm vm)
    {
        if (vm.IsChanging) return;
        vm.IsChanging = true;
        try
        {
            Result result;
            if (vm.IsEnabled)
                result = await _modules.EnableModulesAsync(new[] { vm.Key });
            else
                result = await _modules.DisableModulesAsync(new[] { vm.Key });

            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage;
                vm.IsEnabled = !vm.IsEnabled;
            }
            else
            {
                StatusMessage = $"{vm.DisplayName} {(vm.IsEnabled ? "enabled" : "disabled")}.";
                await LoadAsync();
            }
        }
        finally { vm.IsChanging = false; }
    }

    protected override Task<Result> SaveCoreAsync(CancellationToken ct)
        => Task.FromResult(Result.Ok());
}


