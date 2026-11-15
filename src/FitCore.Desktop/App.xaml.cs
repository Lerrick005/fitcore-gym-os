using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FitCore.Application.Setup;
using FitCore.Desktop.ViewModels.Login;
using FitCore.Desktop.ViewModels.Wizard;
using FitCore.Desktop.Views.Login;
using FitCore.Desktop.Views.Shell;
using FitCore.Desktop.Views.Wizard;
using FitCore.Infrastructure;
using FitCore.Infrastructure.Hosting;
using FitCore.Infrastructure.Identity;
using FitCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;

namespace FitCore.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    public IServiceProvider? Services => _host?.Services;
    private SingleInstanceGuard? _guard;
    private string _dataDir = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LiveCharts2 initialization (once) — UseDefaults registers SkiaSharp, mappers, and light theme
        LiveChartsCore.LiveCharts.Configure(config => config.UseDefaults());

        LoadDisplayFont();

        _guard = new SingleInstanceGuard();
        if (!_guard.TryAcquire())
        {
            MessageBox.Show("FitCore is already running.", "FitCore", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FitCore");
        Directory.CreateDirectory(_dataDir);
        var logDir = Path.Combine(_dataDir, "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logDir, "fitcore-.log"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("FitCore starting. Version={Version}", typeof(App).Assembly.GetName().Version);

        var dbPath = Path.Combine(_dataDir, "fitcore.db");
        Log.Information("Database path: {DbPath}", dbPath);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddFitCoreInfrastructure(dbPath);
                services.AddWizardViewModels();
            })
            .Build();

        try
        {
            _host.StartAsync().GetAwaiter().GetResult();

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FitCoreDbContext>();
            db.Database.Migrate();
            Log.Information("Database migrated.");

            var seeder = scope.ServiceProvider.GetRequiredService<FitCore.Application.Authorization.IRoleSeeder>();
            var seedResult = seeder.SeedAsync().GetAwaiter().GetResult();
            if (!seedResult.IsSuccess)
                Log.Error("Role seeding failed: {Code} {Message}", seedResult.ErrorCode, seedResult.ErrorMessage);
            else
                Log.Information("Roles and permissions seeded.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host failed to start.");
            MessageBox.Show("FitCore failed to start. See logs for details.",
                "FitCore", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        bool completed;
        try
        {
            using var scope = _host.Services.CreateScope();
            var inst = scope.ServiceProvider.GetRequiredService<IInstallationService>();
            completed = inst.IsSetupCompletedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Setup check failed.");
            MessageBox.Show("FitCore failed to check setup state.", "FitCore", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        if (completed)
        {
            ShowLoginThenMain();
        }
        else
        {
            ShowWizard();
        }
    }

    private void ShowLoginThenMain()
    {
        Log.Information("Setup complete. Showing login.");
        _loginScope?.Dispose();
        _loginScope = _host!.Services.CreateScope();
        var loginVm = new LoginViewModel(_loginScope.ServiceProvider.GetRequiredService<FitCore.Application.Identity.IAuthenticationService>());
        var loginWindow = new LoginWindow { DataContext = loginVm };
        loginVm.LoginSucceeded += async (_, _) =>
        {
            Log.Information("Login succeeded. Opening shell.");
            ApplyTheme(_host!.Services);
            await OpenShellAsync();
            loginWindow.Close();
            _loginScope?.Dispose();
            _loginScope = null;
        };
        MainWindow = loginWindow;
        loginWindow.Show();
    }

    private async Task OpenShellAsync()
    {
        _shellScope?.Dispose();
        _shellScope = _host!.Services.CreateScope();
        var shellVm = _shellScope.ServiceProvider.GetRequiredService<FitCore.Desktop.ViewModels.Shell.ShellViewModel>();
        var shell = new ShellWindow { DataContext = shellVm };

        shellVm.LogoutRequested += (_, __) =>
        {
            Log.Information("Logout requested. Returning to login.");
            // Resolve auth to clear session
            _loginScope?.Dispose();
            _loginScope = _host.Services.CreateScope();
            var auth = _loginScope.ServiceProvider.GetRequiredService<FitCore.Application.Identity.IAuthenticationService>();
            auth.Logout();

            var loginVm = new LoginViewModel(auth);
            var login = new LoginWindow { DataContext = loginVm };
            loginVm.LoginSucceeded += async (_, _) =>
            {
                ApplyTheme(_host.Services);
                await OpenShellAsync();
                login.Close();
            };
            MainWindow = login;
            login.Show();
            shell.Close();
        };

        await shellVm.InitializeAsync();
        MainWindow = shell;
        shell.Show();
    }

    private IServiceScope? _wizardScope;
    private IServiceScope? _loginScope;
    private IServiceScope? _shellScope;

    private void ShowWizard()
    {
        Log.Information("Setup incomplete. Opening wizard.");
        _wizardScope = _host!.Services.CreateScope();
        var shellVm = _wizardScope.ServiceProvider.GetRequiredService<WizardShellViewModel>();
        var wizard = new WizardWindow { DataContext = shellVm };
        wizard.Loaded += async (_, __) =>
        {
            try { await shellVm.InitializeAsync(); }
            catch (Exception ex) { Log.Fatal(ex, "Wizard initialize failed."); }
        };

        shellVm.SetupCompleted += async (_, __) =>
        {
            Log.Information("Setup complete. Opening shell.");
            ApplyTheme(_host!.Services);
            await OpenShellAsync();
            wizard.Close();
            _wizardScope?.Dispose();
            _wizardScope = null;
        };

        MainWindow = wizard;
        wizard.Show();
    }

    private static void ApplyTheme(IServiceProvider services)
    {
        try
        {
            var theme = services.GetRequiredService<FitCore.Application.Theming.IThemeService>();
            theme.ApplyFromSettingsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Theme apply failed; falling back to default brand color.");
        }
    }

    private static void LoadDisplayFont()
    {
        try
        {
            var fontUri = new Uri("pack://application:,,,/Fonts/Fraunces-SemiBold.ttf");
            var candidates = new[]
            {
                "Fraunces", "Fraunces SemiBold", "Fraunces 9pt SemiBold",
                "Fraunces SemiBold 9pt", "Fraunces-SemiBold",
            };
            foreach (var name in candidates)
            {
                try
                {
                    var ff = new FontFamily(fontUri, "./#" + name);
                    Current.Resources["Font.Display"] = ff;
                    Log.Information("Display font loaded: {Name}", name);
                    return;
                }
                catch { }
            }
            Log.Warning("Could not load Fraunces; using system serif fallback.");
            Current.Resources["Font.Display"] = new FontFamily("Georgia");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Font load failed; using system serif fallback.");
            try { Current.Resources["Font.Display"] = new FontFamily("Georgia"); } catch { }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("FitCore exiting.");
        try { _host?.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult(); }
        catch { }
        _host?.Dispose();
        Log.CloseAndFlush();
        _guard?.Dispose();
        base.OnExit(e);
    }
}








