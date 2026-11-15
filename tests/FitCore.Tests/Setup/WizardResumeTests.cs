using System.Threading.Tasks;
using FitCore.Application.Setup;
using FitCore.Desktop.ViewModels.Wizard;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Setup;

public class WizardResumeTests
{
    [Fact]
    public async Task FreshInstall_StartsAtStep1()
    {
        using var host = new TestHost();
        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell.InitializeAsync();

        shell.CurrentStepIndex.ShouldBe(0);
        shell.CurrentStepViewModel.ShouldBeOfType<BusinessIdentityViewModel>();
    }

    [Fact]
    public async Task ResumeMidWizard_StartsAtSavedStep()
    {
        using var host = new TestHost();
        using (var scope = host.CreateScope())
        {
            var inst = scope.ServiceProvider.GetRequiredService<IInstallationService>();
            await inst.GetOrCreateAsync();
            await inst.SetStepAsync(2); // 3-step wizard: 1=YourGym, 2=Account, 3=Confirm
        }

        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell.InitializeAsync();

        shell.CurrentStepIndex.ShouldBe(1); // index 1 = step 2 = Account
        shell.CurrentStepViewModel.ShouldBeOfType<RolesAndOwnerViewModel>();
    }

    [Fact]
    public async Task CompletedSetup_RaisesEventAndLoadsNoStep()
    {
        using var host = new TestHost();
        using (var scope = host.CreateScope())
        {
            var inst = scope.ServiceProvider.GetRequiredService<IInstallationService>();
            await inst.GetOrCreateAsync();
            await inst.CompleteSetupAsync();
        }

        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        var fired = false;
        shell.SetupCompleted += (_, __) => fired = true;
        await shell.InitializeAsync();

        fired.ShouldBeTrue();
        shell.CurrentStepViewModel.ShouldBeNull();
    }
}

