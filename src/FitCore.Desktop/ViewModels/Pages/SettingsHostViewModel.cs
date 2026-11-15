using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Theming;
using FitCore.Desktop.ViewModels.Pages.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class SettingsHostViewModel : PageViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;

    [ObservableProperty] private PageViewModelBase? _current;
    public bool IsOnLanding => Current == Landing;
    [ObservableProperty] private SettingsViewModel? _landing;

    public override string Title => "Settings";

    public SettingsHostViewModel(IServiceScopeFactory scopeFactory, ISettingsService settings, IThemeService theme)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _theme = theme;
    }

    public override async Task LoadAsync(System.Threading.CancellationToken ct = default)
    {
        Landing = new SettingsViewModel();
        Landing.SectionSelected += async (_, key) => await OpenSectionAsync(key);
        Current = Landing;
        await Task.CompletedTask;
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.PlansSettingsViewModel _pageScope_Plans()
    {
        var scope = _scopeFactory.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.PlansSettingsViewModel>();
        vm.EditRequested += async (_, id) => await OpenPlanEditAsync(id);
        vm.ArchiveRequested += async (_, id) =>
        {
            var svc = scope.ServiceProvider.GetRequiredService<FitCore.Application.Plans.IMembershipPlanService>();
            var r = await svc.ArchiveAsync(id);
            if (r.IsSuccess) await vm.LoadAsync();
        };
        vm.RestoreRequested += async (_, id) =>
        {
            var svc = scope.ServiceProvider.GetRequiredService<FitCore.Application.Plans.IMembershipPlanService>();
            var r = await svc.RestoreAsync(id);
            if (r.IsSuccess) await vm.LoadAsync();
        };
        return vm;
    }

    private async Task OpenPlanEditAsync(int? planId)
    {
        var scope = _scopeFactory.CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.PlanEditViewModel>();
        var window = new FitCore.Desktop.Views.Pages.Settings.PlanEditWindow
        {
            DataContext = editVm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        editVm.CloseRequested += (_, ok) =>
        {
            window.Close();
            if (ok && Current is FitCore.Desktop.ViewModels.Pages.Settings.PlansSettingsViewModel p)
                _ = p.LoadAsync();
        };
        await editVm.InitializeAsync(planId);
        window.ShowDialog();
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.UsersSettingsViewModel _pageScope_Users()
    {
        var scope = _scopeFactory.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.UsersSettingsViewModel>();
        vm.EditRequested += async (_, id) => await OpenUserEditAsync(id);
        vm.ToggleActiveRequested += async (_, id) =>
        {
            var users = scope.ServiceProvider.GetRequiredService<FitCore.Application.Identity.IUserManagementService>();
            var list = await users.ListUsersAsync();
            if (!list.IsSuccess) return;
            var user = list.Value!.FirstOrDefault(u => u.Id == id);
            if (user is null) return;
            await users.SetActiveAsync(id, !user.IsActive, "settings toggle");
            await vm.LoadAsync();
        };
        return vm;
    }

    private async Task OpenUserEditAsync(int? userId)
    {
        var scope = _scopeFactory.CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.UserEditViewModel>();
        var window = new FitCore.Desktop.Views.Pages.Settings.UserEditWindow
        {
            DataContext = editVm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        editVm.CloseRequested += (_, ok) =>
        {
            window.Close();
            if (ok && Current is FitCore.Desktop.ViewModels.Pages.Settings.UsersSettingsViewModel u)
                _ = u.LoadAsync();
        };
        await editVm.InitializeAsync(userId);
        window.ShowDialog();
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.ModulesSettingsViewModel _pageScope_Modules()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.ModulesSettingsViewModel>();
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.LicenceSettingsViewModel _pageScope_Licence()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.LicenceSettingsViewModel>();
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.BackupSettingsViewModel _pageScope_Backup()
    {
        var scope = _scopeFactory.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.BackupSettingsViewModel>();
        return vm;
    }

    private async Task OpenSectionAsync(string key)
    {
        SettingsPageViewModelBase? vm = key switch
        {
            "branding" => new BrandingSettingsViewModel(_settings, _theme),
            "business" => new BusinessSettingsViewModel(_settings),
            "locale" => new LocaleSettingsViewModel(_settings),
            "hours" => new HoursSettingsViewModel(_settings),
            "plans" => _pageScope_Plans(),
            "users" => _pageScope_Users(),
            "backup" => _pageScope_Backup(),
            "licence" => _pageScope_Licence(),
            "modules" => _pageScope_Modules(),
            "rewards" => _pageScope_Rewards(),
            _ => null
        };
        if (vm is null) return;
        await vm.LoadAsync();
        Current = vm;
    }

    partial void OnCurrentChanged(PageViewModelBase? value)
    {
        OnPropertyChanged(nameof(IsOnLanding));
    }

    [RelayCommand]
    private void BackToLanding()
    {
        Current = Landing;
    }

    private FitCore.Desktop.ViewModels.Pages.Settings.RewardsSettingsViewModel _pageScope_Rewards()
    {
        var scope = _scopeFactory.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.RewardsSettingsViewModel>();
        vm.EditRequested += async (_, id) => await OpenRewardEditAsync(id);
        return vm;
    }

    private async Task OpenRewardEditAsync(int? rewardId)
    {
        var scope = _scopeFactory.CreateScope();
        var editVm = scope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Modules.RewardEditViewModel>();
        var window = new FitCore.Desktop.Views.Modules.RewardEditWindow
        {
            DataContext = editVm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        editVm.CloseRequested += (_, ok) =>
        {
            window.Close();
            if (ok && Current is FitCore.Desktop.ViewModels.Pages.Settings.RewardsSettingsViewModel r)
                _ = r.LoadAsync();
        };
        await editVm.InitializeAsync(rewardId);
        window.ShowDialog();
    }
}








