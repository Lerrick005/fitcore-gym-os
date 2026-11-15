using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Navigation;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Wizard;
using Microsoft.Extensions.DependencyInjection;
using FitCore.Infrastructure.Classes;

namespace FitCore.Desktop.ViewModels.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUser _user;
    private readonly ISessionGenerationService _sessionGen;
    private IServiceScope? _pageScope;

    [ObservableProperty] private ObservableCollection<NavGroupViewModel> _groups = new();
    [ObservableProperty] private PageViewModelBase? _currentPage;
    [ObservableProperty] private string _currentPageKey = "dashboard";
    [ObservableProperty] private bool _isSidebarCollapsed;
    [ObservableProperty] private string _userDisplayName = string.Empty;
    [ObservableProperty] private bool _showAttribution = true;
    [ObservableProperty] private string _attributionText = "Powered by FitCore";
    [ObservableProperty] private string _gymName = string.Empty;

    public event EventHandler? LogoutRequested;

    public ShellViewModel(INavigationService nav, IServiceScopeFactory scopeFactory, ICurrentUser user, ISessionGenerationService sessionGen)
    {
        _nav = nav;
        _scopeFactory = scopeFactory;
        _user = user;
        _sessionGen = sessionGen;
        _userDisplayName = user.Username ?? "Guest";

        _nav.NavigationChanged += async (_, __) => await OnNavigationChangedAsync();
    }

    public async Task InitializeAsync()
    {
        // Refresh user display name now that session is active
        try
        {
            await _sessionGen.EnsureSessionsAsync(30);
        }
        catch
        {
            // Session generation is best-effort; don't crash the app if it fails
        }

        UserDisplayName = _user.Username ?? "Signed in";
        await _nav.RefreshVisibleItemsAsync();
        await OnNavigationChangedAsync();
    }

    private async Task OnNavigationChangedAsync()
    {
        var grouped = _nav.VisibleItems
            .GroupBy(item => string.IsNullOrEmpty(item.Group) ? "Main" : item.Group)
            .OrderBy(g => GroupOrder(g.Key));

        var newGroups = new ObservableCollection<NavGroupViewModel>();
        foreach (var g in grouped)
        {
            var groupVm = new NavGroupViewModel(g.Key);
            foreach (var item in g)
            {
                var itemVm = new NavItemViewModel(item);
                itemVm.IsActive = item.Key == _nav.CurrentPageKey;
                groupVm.Items.Add(itemVm);
            }
            newGroups.Add(groupVm);
        }
        Groups = newGroups;

        if (_nav.CurrentPageKey != CurrentPageKey || CurrentPage is null)
        {
            CurrentPageKey = _nav.CurrentPageKey;
            await LoadPageAsync(CurrentPageKey);
        }
    }

    private async Task<PageViewModelBase?> LoadMemberDetailAsync()
    {
        var memberId = _nav is FitCore.Desktop.Navigation.ShellNavigationService navSvc ? navSvc.CurrentMemberId : (int?)null;
        if (memberId is null) return null;

        var detailVm = _pageScope!.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Members.MemberDetailViewModel>();
        await detailVm.InitializeAsync(memberId.Value);
        return detailVm;
    }

    private async Task<PageViewModelBase?> LoadClassAttendeesAsync()
    {
        if (_nav is not FitCore.Desktop.Navigation.ShellNavigationService navSvc) return null;
        if (!navSvc.CurrentProgrammeId.HasValue) return null;

        var vm = _pageScope!.ServiceProvider.GetRequiredService<ClassAttendeesViewModel>();
        vm.SetProgramme(navSvc.CurrentProgrammeId.Value, navSvc.CurrentAttendeesClassName, navSvc.CurrentAttendeesSchedule, navSvc.CurrentAttendeesCapacity);
        await vm.LoadAsync();
        return vm;
    }

    private async Task LoadPageAsync(string pageKey)
    {
        _pageScope?.Dispose();
        _pageScope = _scopeFactory.CreateScope();

        PageViewModelBase? vm = pageKey switch
        {
            "dashboard" => _pageScope.ServiceProvider.GetRequiredService<DashboardViewModel>(),
            "members" => _pageScope.ServiceProvider.GetRequiredService<MembersViewModel>(),
            "payments" => _pageScope.ServiceProvider.GetRequiredService<PaymentsViewModel>(),
            "plans" => _pageScope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Pages.Settings.PlansSettingsViewModel>(),
            "attendance" => _pageScope.ServiceProvider.GetRequiredService<AttendanceViewModel>(),
            "cashup" => _pageScope.ServiceProvider.GetRequiredService<CashUpViewModel>(),
            "trainers" => _pageScope.ServiceProvider.GetRequiredService<TrainersViewModel>(),
            "expenses" => _pageScope.ServiceProvider.GetRequiredService<ExpensesViewModel>(),
            "inventory" => _pageScope.ServiceProvider.GetRequiredService<InventoryViewModel>(),
            "retention" => _pageScope.ServiceProvider.GetRequiredService<RetentionViewModel>(),
            "loyalty" => _pageScope.ServiceProvider.GetRequiredService<LoyaltyViewModel>(),
            "classes" => _pageScope.ServiceProvider.GetRequiredService<ClassesViewModel>(),
            "fitness" => _pageScope.ServiceProvider.GetRequiredService<FitnessViewModel>(),
            "reports" => _pageScope.ServiceProvider.GetRequiredService<ReportsViewModel>(),
            "settings" => _pageScope.ServiceProvider.GetRequiredService<SettingsHostViewModel>(),
            "member-detail" => await LoadMemberDetailAsync(),
            "class-attendees" => await LoadClassAttendeesAsync(),
            _ => null
        };

        if (vm is null) return;
        await vm.LoadAsync();

        // Wire member list → member detail navigation
        if (vm is MembersViewModel mv)
        {
            mv.OpenDetailRequested += async (_, memberId) =>
            {
                await _nav.NavigateToMemberAsync(memberId);
            };
        }

        // Wire member detail → back navigation
        if (vm is FitCore.Desktop.ViewModels.Members.MemberDetailViewModel md)
        {
            md.BackRequested += async (_, __) =>
            {
                await _nav.NavigateAsync("members");
            };
        }

        CurrentPage = vm;
    }

    [RelayCommand]
    private async Task NavigateToAsync(NavItemViewModel? item)
    {
        if (item is null) return;
        await _nav.NavigateAsync(item.Key);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    [RelayCommand]
    private void Logout()
    {
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private static int GroupOrder(string group) => group switch
    {
        "Main" => 0,
        "Operations" => 1,
        "Management" => 2,
        "System" => 3,
        _ => 99
    };
}



















