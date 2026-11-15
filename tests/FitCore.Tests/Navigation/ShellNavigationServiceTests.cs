using System;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Navigation;
using FitCore.Desktop.Navigation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Navigation;

public class ShellNavigationServiceTests
{
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IModuleAccessGuard _modules = Substitute.For<IModuleAccessGuard>();

    private ShellNavigationService NewService() => new(_user, _modules);

    [Fact]
    public async Task RefreshVisibleItems_FiltersByPermission()
    {
        _user.HasPermission("members.view").Returns(false);
        _user.HasPermission("settings.view").Returns(false);
        _user.HasPermission("payments.record").Returns(true);
        _user.HasPermission("attendance.checkin").Returns(true);
        _user.HasPermission("reports.view").Returns(true);

        var svc = NewService();
        await svc.RefreshVisibleItemsAsync();

        var keys = System.Linq.Enumerable.Select(svc.VisibleItems, i => i.Key).ToList();
        keys.ShouldContain("dashboard");     // no permission required
        keys.ShouldContain("payments");
        keys.ShouldContain("attendance");
        keys.ShouldContain("reports");
        keys.ShouldNotContain("members");
        keys.ShouldNotContain("settings");
    }

    [Fact]
    public async Task RefreshVisibleItems_FiltersByModuleState()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(true);
        _modules.IsEnabledAsync("modules", Arg.Any<System.Threading.CancellationToken>()).Returns(false);

        // all core items visible
        var svc = NewService();
        await svc.RefreshVisibleItemsAsync();

        // All core pages available since no RequiredModule on them
        svc.VisibleItems.Count.ShouldBe(8);
    }

    [Fact]
    public async Task NavigateToVisiblePage_ChangesCurrentPage()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(true);

        var svc = NewService();
        await svc.RefreshVisibleItemsAsync();
        await svc.NavigateAsync("members");

        svc.CurrentPageKey.ShouldBe("members");
    }

    [Fact]
    public async Task NavigateToInvisiblePage_IsIgnored()
    {
        _user.HasPermission("members.view").Returns(false);
        _user.HasPermission(Arg.Is<string>(s => s != "members.view")).Returns(true);

        var svc = NewService();
        await svc.RefreshVisibleItemsAsync();
        var original = svc.CurrentPageKey;

        await svc.NavigateAsync("members");

        svc.CurrentPageKey.ShouldBe(original);
    }

    [Fact]
    public async Task Refresh_FallsBackToDashboard_WhenCurrentPageDisappears()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(true);

        var svc = NewService();
        await svc.RefreshVisibleItemsAsync();
        await svc.NavigateAsync("members");
        svc.CurrentPageKey.ShouldBe("members");

        // Now remove members permission
        _user.HasPermission("members.view").Returns(false);
        await svc.RefreshVisibleItemsAsync();

        svc.CurrentPageKey.ShouldBe("dashboard");
    }
}

