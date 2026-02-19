using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Events;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Tests.Commands;

public class LoginUserHandlerTests
{
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private LoginUserHandler CreateSut() => new(_loginService, _eventBus);

    [Fact]
    public async Task HandleAsync_ValidPin_ReturnsSuccessAndPublishesEvent()
    {
        _loginService.ValidatePinAsync(UserType.Admin, "1234")
            .Returns(LoginResult.Success());

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.Admin, "1234"));

        Assert.True(result.Succeeded);
        await _eventBus.Received(1).PublishAsync(Arg.Is<UserLoggedInEvent>(e =>
            e.UserType == UserType.Admin));
    }

    [Fact]
    public async Task HandleAsync_InvalidPin_ReturnsFailureWithRemainingAttempts()
    {
        _loginService.ValidatePinAsync(UserType.User, "0000")
            .Returns(LoginResult.Failed("Invalid PIN. 2 attempt(s) remaining.", 2));

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.User, "0000"));

        Assert.False(result.Succeeded);
        Assert.Contains("2 attempt(s) remaining", result.ErrorMessage);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<UserLoggedInEvent>());
    }

    [Fact]
    public async Task HandleAsync_LockedOut_ReturnsFailureWithLockoutMessage()
    {
        _loginService.ValidatePinAsync(UserType.Manager, "9999")
            .Returns(LoginResult.LockedOut(DateTime.UtcNow.AddMinutes(2)));

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.Manager, "9999"));

        Assert.False(result.Succeeded);
        Assert.Contains("Account locked", result.ErrorMessage);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<UserLoggedInEvent>());
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ReturnsFailure()
    {
        _loginService.ValidatePinAsync(UserType.User, "1111")
            .Returns(LoginResult.Failed("User not found.", 0));

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.User, "1111"));

        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.ErrorMessage);
    }
}
