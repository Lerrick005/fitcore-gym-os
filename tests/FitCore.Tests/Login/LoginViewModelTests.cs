using System;
using System.Threading.Tasks;
using FitCore.Application.Common;
using FitCore.Application.Identity;
using FitCore.Desktop.ViewModels.Login;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FitCore.Tests.Login;

public class LoginViewModelTests
{
    private readonly IAuthenticationService _auth = Substitute.For<IAuthenticationService>();

    [Fact]
    public async Task SignIn_EmptyUsername_ShowsError()
    {
        var vm = new LoginViewModel(_auth);
        vm.Password = "anything";
        await vm.SignInCommand.ExecuteAsync(null);
        vm.ErrorMessage.ShouldBe("Username is required.");
        await _auth.DidNotReceive().LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task SignIn_EmptyPassword_ShowsError()
    {
        var vm = new LoginViewModel(_auth);
        vm.Username = "alice";
        await vm.SignInCommand.ExecuteAsync(null);
        vm.ErrorMessage.ShouldBe("Password is required.");
    }

    [Fact]
    public async Task SignIn_ValidCredentials_RaisesLoginSucceeded()
    {
        _auth.LoginAsync("alice", "Str0ng!Passw0rd", Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Ok(new AuthenticatedUser(1, "alice", "Alice", 1)));

        var vm = new LoginViewModel(_auth);
        vm.Username = "alice";
        vm.Password = "Str0ng!Passw0rd";

        AuthenticatedUser? raised = null;
        vm.LoginSucceeded += (_, u) => raised = u;

        await vm.SignInCommand.ExecuteAsync(null);

        raised.ShouldNotBeNull();
        raised!.Username.ShouldBe("alice");
        vm.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SignIn_InvalidCredentials_ShowsServiceError()
    {
        _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Fail("auth.invalid", "Invalid username or password."));

        var vm = new LoginViewModel(_auth);
        vm.Username = "alice";
        vm.Password = "wrong";
        await vm.SignInCommand.ExecuteAsync(null);

        vm.ErrorMessage.ShouldBe("Invalid username or password.");
    }
}
