using FitCore.Application.Abstractions;
using FitCore.Application.Attendance;
using FitCore.Application.Authorization;
using FitCore.Application.Backup;
using FitCore.Application.CashUp;
using FitCore.Application.Classes;
using FitCore.Application.Dashboard;
using FitCore.Application.Expenses;
using FitCore.Application.Exporting;
using FitCore.Application.Formatting;
using FitCore.Application.Identity;
using FitCore.Application.Importing;
using FitCore.Application.Inventory;
using FitCore.Application.Licensing;
using FitCore.Application.Loyalty;
using FitCore.Application.Members;
using FitCore.Application.Memberships;
using FitCore.Application.Modules;
using FitCore.Application.Payments;
using FitCore.Application.Photos;
using FitCore.Application.Plans;
using FitCore.Application.Receipts;
using FitCore.Application.Recovery;
using FitCore.Application.Reports;
using FitCore.Application.Retention;
using FitCore.Application.Session;
using FitCore.Application.Settings;
using FitCore.Application.Setup;
using FitCore.Application.Theming;
using FitCore.Application.Training;
using FitCore.Infrastructure.Attendance;
using FitCore.Infrastructure.Authorization;
using FitCore.Infrastructure.Backup;
using FitCore.Infrastructure.CashUp;
using FitCore.Infrastructure.Classes;
using FitCore.Infrastructure.Cryptography;
using FitCore.Infrastructure.Dashboard;
using FitCore.Infrastructure.Expenses;
using FitCore.Infrastructure.Exporting;
using FitCore.Infrastructure.Formatting;
using FitCore.Infrastructure.Identity;
using FitCore.Infrastructure.Importing;
using FitCore.Infrastructure.Inventory;
using FitCore.Infrastructure.Licensing;
using FitCore.Infrastructure.Loyalty;
using FitCore.Infrastructure.Members;
using FitCore.Infrastructure.Memberships;
using FitCore.Infrastructure.Modules;
using FitCore.Infrastructure.Payments;
using FitCore.Infrastructure.Photos;
using FitCore.Infrastructure.Plans;
using FitCore.Infrastructure.Receipts;
using FitCore.Infrastructure.Recovery;
using FitCore.Infrastructure.Reports;
using FitCore.Infrastructure.Retention;
using FitCore.Infrastructure.Session;
using FitCore.Infrastructure.Setup;
using FitCore.Infrastructure.Settings;
using FitCore.Infrastructure.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using FitCore.Infrastructure.Persistence;
using FitCore.Infrastructure.Services;
namespace FitCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFitCoreInfrastructure(
        this IServiceCollection services,
        string sqliteDbPath)
    {
        services.AddDbContext<FitCoreDbContext>(opts =>
            opts.UseSqlite($"Data Source={sqliteDbPath}")
                .AddInterceptors(new SqlitePragmaInterceptor()));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<CurrentUser>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddScoped<IModuleAccessGuard, ModuleAccessGuard>();
        services.AddScoped<IModuleSetupService, ModuleSetupService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IInstallationService, InstallationService>();

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IUserCreationService, UserCreationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddSingleton<ISignatureVerifier, RsaSignatureVerifier>();
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICashUpService, CashUpService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IMembershipPlanService, MembershipPlanService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IMemberImportService, MemberImportService>();
        services.AddScoped<IDefaultsProvider>(_ =>
        {
            var defaultsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "defaults", "fitcore.defaults.json");
            return new JsonDefaultsProvider(defaultsPath);
        });
        services.AddScoped<IRoleSeeder, RoleSeeder>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IReportExporter, ReportExporter>();
        services.AddScoped<ICurrencyFormatter, LocaleCurrencyFormatter>();
        services.AddScoped<IDateTimeFormatter, LocaleDateTimeFormatter>();
        services.AddScoped<ILicenceService, LicenceService>();

        // Optional modules
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IRetentionService, RetentionService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<ILoyaltyRewardService, LoyaltyRewardService>();
        services.AddScoped<IClassesService, ClassesService>();

        // Photo capture
        services.AddSingleton<IPhotoCaptureService>(sp =>
        {
            var dataFolder = System.IO.Path.GetDirectoryName(sqliteDbPath) ?? sqliteDbPath;
            return new FileOnlyPhotoService(dataFolder);
        });

        // Session services
        services.AddSingleton<BackupScheduler>();
        services.AddSingleton<IBackupScheduler>(sp => sp.GetRequiredService<BackupScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<BackupScheduler>());
        services.AddSingleton<IAutoLockService, AutoLockService>();

        // Backup service needs IBackupService registered too
        services.AddSingleton<IBackupService>(sp =>
        {
            var dataFolder = System.IO.Path.GetDirectoryName(sqliteDbPath) ?? sqliteDbPath;
            return new BackupService(sp.GetRequiredService<IServiceScopeFactory>(), sqliteDbPath);
        });

        return services;
    }
}



