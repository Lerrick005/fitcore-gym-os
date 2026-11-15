using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Dashboard;
using FitCore.Application.Settings; 
using FitCore.Application.Abstractions;
using FitCore.Application.Navigation;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class DashboardViewModel : PageViewModelBase
{
    private readonly IDashboardService _dashboard;
    private readonly ISettingsService _settings;
    private readonly INavigationService _nav;

    [ObservableProperty] private string _greeting = string.Empty;
    [ObservableProperty] private string _businessName = string.Empty;

    [ObservableProperty] private int _activeMembers;
    [ObservableProperty] private int _expiringSoon;
    [ObservableProperty] private int _expired;
    [ObservableProperty] private int _todayCheckIns;
    [ObservableProperty] private string _todayRevenueDisplay = "0.00";        
    [ObservableProperty] private int _outstanding;
    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<string> RecentActivity { get; } = new();

    public override string Title => "Dashboard";

    public DashboardViewModel(IDashboardService dashboard, ISettingsService settings, INavigationService nav)
    {
        _dashboard = dashboard;     
        _settings = settings;       
        _nav = nav;
    }

    public override async Task LoadAsync(CancellationToken ct = default)      
    {
        IsLoading = true;
        try
        {
            var branding = await _settings.GetAsync<BrandingSettings>(ct);    
            BusinessName = branding.BusinessName;
            Greeting = BuildGreeting(branding.BusinessName);

            var stats = await _dashboard.GetStatsAsync(ct);
            ActiveMembers = stats.ActiveMembers;
            ExpiringSoon = stats.ExpiringSoonMembers;
            Expired = stats.ExpiredMembers;
            TodayCheckIns = stats.TodayCheckIns;
            Outstanding = stats.OutstandingBalancesCount;
            TotalMembers = stats.TotalMembers;
            TodayRevenueDisplay = FormatMoney(stats.TodayRevenueMinor, stats.CurrencyCode);
        }
        finally
        {
            IsLoading = false;      
        }
    }

    private static string BuildGreeting(string businessName)
    {
        if (string.IsNullOrWhiteSpace(businessName)) return "Welcome to FitCore";
        return $"Welcome, {businessName}";
    }

    private static string FormatMoney(long amountMinor, string currencyCode)  
    {
        var amount = amountMinor / 100m;
        return $"{amount:N2} {currencyCode}";
    }

    [RelayCommand]
    private async Task AddMember() => await _nav.NavigateAsync("members");

    [RelayCommand]
    private async Task QuickSale() => await _nav.NavigateAsync("payments");

    [RelayCommand]
    private async Task RecordPayment() => await _nav.NavigateAsync("payments");

    [RelayCommand]
    private async Task CheckInMember() => await _nav.NavigateAsync("attendance");
}
