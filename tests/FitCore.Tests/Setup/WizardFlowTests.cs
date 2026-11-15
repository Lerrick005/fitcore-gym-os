using System.Threading.Tasks;
using FitCore.Application.Setup;
using FitCore.Desktop.ViewModels.Wizard;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Setup;

public class WizardFlowTests
{
    [Fact]
    public async Task Next_OnInvalidStep1_DoesNotAdvance()
    {
        using var host = new TestHost();
        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell.InitializeAsync();

        // BusinessName empty => validation fails
        await shell.NextCommand.ExecuteAsync(null);

        shell.CurrentStepIndex.ShouldBe(0);
        shell.CurrentStepViewModel!.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task Next_OnValidStep1_AdvancesAndPersists()
    {
        using var host = new TestHost();
        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell.InitializeAsync();

        var vm = (BusinessIdentityViewModel)shell.CurrentStepViewModel!;
        vm.BusinessName = "Acme Gym";
        vm.PhonePrimary = "0700000000";

        await shell.NextCommand.ExecuteAsync(null);

        shell.CurrentStepIndex.ShouldBe(1);
        shell.CurrentStepViewModel.ShouldBeOfType<RolesAndOwnerViewModel>();

        using var scope = host.CreateScope();
        var inst = scope.ServiceProvider.GetRequiredService<IInstallationService>();
        (await inst.GetAsync()).SetupStep.ShouldBe(2);
    }

    [Fact]
    public async Task Back_FromStep2_ReturnsToStep1()
    {
        using var host = new TestHost();
        var shell = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell.InitializeAsync();

        var vm = (BusinessIdentityViewModel)shell.CurrentStepViewModel!;
        vm.BusinessName = "Acme Gym";
        vm.PhonePrimary = "0700000000";
        await shell.NextCommand.ExecuteAsync(null);
        shell.CurrentStepIndex.ShouldBe(1);

        await shell.BackCommand.ExecuteAsync(null);

        shell.CurrentStepIndex.ShouldBe(0);
        shell.CurrentStepViewModel.ShouldBeOfType<BusinessIdentityViewModel>();
    }

    [Fact]
    public async Task Resume_AfterRestartMidWizard_LoadsCorrectStep()
    {
        using var host = new TestHost();

        // First "run": advance to step 2, then dispose shell (simulating app close)
        {
            var shell1 = host.Provider.GetRequiredService<WizardShellViewModel>();
            await shell1.InitializeAsync();
            var vm = (BusinessIdentityViewModel)shell1.CurrentStepViewModel!;
            vm.BusinessName = "Acme Gym";
            vm.PhonePrimary = "0700000000";
            await shell1.NextCommand.ExecuteAsync(null);
            shell1.CurrentStepIndex.ShouldBe(1);
        }

        // Second "run": new shell instance
        var shell2 = host.Provider.GetRequiredService<WizardShellViewModel>();
        await shell2.InitializeAsync();
        shell2.CurrentStepIndex.ShouldBe(1);
        shell2.CurrentStepViewModel.ShouldBeOfType<RolesAndOwnerViewModel>();
    }
}

