using System.Threading.Tasks;
using FitCore.Application.Identity;
using FitCore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FitCore.Tests.UserSetup;

public class UserCreationTests
{
    [Fact]
    public async Task CreateFirstOwner_Succeeds_OnEmptyDb()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        var result = await svc.CreateFirstOwnerAsync("owner1", "Owner One", "Str0ng!Passw0rd");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Username.ShouldBe("owner1");
    }

    [Fact]
    public async Task CreateFirstOwner_AutoCreatesOwnerRole()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitCoreDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Roles)).ShouldBe(0);
        var result = await svc.CreateFirstOwnerAsync("owner1", "Owner One", "Str0ng!Passw0rd");
        result.IsSuccess.ShouldBeTrue();

        var ownerRole = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Roles, r => r.IsOwnerRole);
        ownerRole.Name.ShouldBe("Owner");
    }

    [Fact]
    public async Task CreateSecondOwner_WhenActiveOwnerExists_Fails()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        (await svc.CreateFirstOwnerAsync("owner1", "Owner One", "Str0ng!Passw0rd")).IsSuccess.ShouldBeTrue();

        var second = await svc.CreateFirstOwnerAsync("owner2", "Owner Two", "An0ther!Pass");
        second.IsSuccess.ShouldBeFalse();
        second.ErrorCode.ShouldBe("identity.owner.exists");
    }

    [Fact]
    public async Task CreateOwner_WithWeakPassword_Fails()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        var result = await svc.CreateFirstOwnerAsync("owner1", "Owner One", "weak");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("identity.password.weak");
    }

    [Fact]
    public async Task CreateOwner_WithInvalidUsername_Fails()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        var result = await svc.CreateFirstOwnerAsync("no spaces", "Owner One", "Str0ng!Passw0rd");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("identity.username.invalid");
    }

    [Fact]
    public async Task CreateOwner_AfterOwnerDeactivated_Succeeds()
    {
        using var host = new TestHost();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitCoreDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IUserCreationService>();

        (await svc.CreateFirstOwnerAsync("owner1", "Owner One", "Str0ng!Passw0rd")).IsSuccess.ShouldBeTrue();
        var first = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Users);
        first.IsActive = false;
        await db.SaveChangesAsync();

        // No active Owner now -> creating one should succeed (§11 "at least one active Owner").
        var result = await svc.CreateFirstOwnerAsync("owner2", "Owner Two", "An0ther!Pass");
        result.IsSuccess.ShouldBeTrue();
    }
}






