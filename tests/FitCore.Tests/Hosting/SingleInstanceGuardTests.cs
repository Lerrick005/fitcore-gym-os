using FitCore.Infrastructure.Hosting;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Hosting;

public class SingleInstanceGuardTests
{
    [Fact]
    public void FirstAcquire_Succeeds()
    {
        using var guard = new SingleInstanceGuard();
        guard.TryAcquire().ShouldBeTrue();
    }

    [Fact]
    public void SecondGuard_CannotAcquire()
    {
        using var first = new SingleInstanceGuard();
        first.TryAcquire().ShouldBeTrue();

        using var second = new SingleInstanceGuard();
        second.TryAcquire().ShouldBeFalse();
    }
}
