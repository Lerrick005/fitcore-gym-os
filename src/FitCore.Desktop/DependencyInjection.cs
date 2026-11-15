using FitCore.Application.Navigation;
using FitCore.Application.Theming;
using FitCore.Desktop.Navigation;
using FitCore.Desktop.Theming;
using FitCore.Desktop.ViewModels;
using FitCore.Desktop.ViewModels.Login;
using FitCore.Desktop.ViewModels.Members;
using FitCore.Desktop.ViewModels.Pages;
using FitCore.Desktop.ViewModels.Pages.Settings;
using FitCore.Desktop.ViewModels.Shell;
using FitCore.Desktop.ViewModels.Wizard;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Desktop-side services and view models.
    /// Naming kept as AddWizardViewModels for call-site compatibility — registration list is broader.
    /// </summary>
    public static IServiceCollection AddWizardViewModels(this IServiceCollection services)
    {
        // Wizard step view models
        services.AddTransient<BusinessIdentityViewModel>();
        services.AddTransient<RolesAndOwnerViewModel>();
        services.AddTransient<ReviewViewModel>();
        services.AddTransient<WizardShellViewModel>();

        // Shell + navigation
        services.AddScoped<INavigationService, ShellNavigationService>();
        services.AddTransient<ShellViewModel>();

        // Theme
        services.AddSingleton<IThemeService>(sp => new ThemeService(sp.GetRequiredService<IServiceScopeFactory>()));

        // Login
        services.AddTransient<LoginViewModel>();

        // Pages
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MembersViewModel>();
        services.AddTransient<MemberDetailViewModel>();
        services.AddTransient<SellPlanViewModel>();
        services.AddTransient<RecordPaymentViewModel>();
        services.AddTransient<ReceiptViewModel>();
        services.AddTransient<MemberEditViewModel>();
        services.AddTransient<MemberImportViewModel>();
        services.AddTransient<NewMemberFlowViewModel>();
        services.AddTransient<QuickSaleViewModel>();
        services.AddTransient<PaymentsViewModel>();
        services.AddTransient<AttendanceViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<CashUpViewModel>();
        services.AddTransient<TrainersViewModel>();
        services.AddTransient<ExpensesViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<RetentionViewModel>();
        services.AddTransient<LoyaltyViewModel>();
        services.AddTransient<ClassesViewModel>();
        services.AddTransient<FitnessViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.LogWorkoutViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.LogBodyMetricViewModel>();
        services.AddScoped<FitCore.Infrastructure.Classes.ISessionGenerationService, FitCore.Infrastructure.Classes.SessionGenerationService>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.TrainerEditViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.ExpenseEditViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.InventoryEditViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.StockAdjustmentViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.ReferralCreateViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.LoyaltyEntryAddViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.ClassCreateViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.ClassBookViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Pages.ClassAttendeesViewModel>();
        services.AddTransient<SettingsHostViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Settings pages
        services.AddTransient<BrandingSettingsViewModel>();
        services.AddTransient<BusinessSettingsViewModel>();
        services.AddTransient<LocaleSettingsViewModel>();
        services.AddTransient<BackupSettingsViewModel>();
        services.AddTransient<LicenceSettingsViewModel>();
        services.AddTransient<HoursSettingsViewModel>();
        services.AddTransient<PlansSettingsViewModel>();
        services.AddTransient<UsersSettingsViewModel>();
        services.AddTransient<ModulesSettingsViewModel>();
        services.AddTransient<RewardsSettingsViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.RewardEditViewModel>();
        services.AddTransient<FitCore.Desktop.ViewModels.Modules.FollowUpCompleteViewModel>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<PlanEditViewModel>();

        services.AddScoped<FitCore.Application.Progress.IProgressService, FitCore.Infrastructure.Progress.ProgressService>();
        return services;
    }
}




















