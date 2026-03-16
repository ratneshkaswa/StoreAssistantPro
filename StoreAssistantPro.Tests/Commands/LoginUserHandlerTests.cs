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
        _loginService.ValidatePinAsync(UserType.Admin, "1234", Arg.Any<CancellationToken>())
            .Returns(LoginResult.Success());

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.Admin, "1234"));

        Assert.True(result.Succeeded);
        await _eventBus.Received(1).PublishAsync(Arg.Is<UserLoggedInEvent>(e =>
            e.UserType == UserType.Admin));
    }

    [Fact]
    public async Task HandleAsync_InvalidPin_ReturnsFailure()
    {
        _loginService.ValidatePinAsync(UserType.Admin, "0000", Arg.Any<CancellationToken>())
            .Returns(LoginResult.Failed("Invalid PIN."));

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.Admin, "0000"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid PIN.", result.ErrorMessage);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<UserLoggedInEvent>());
    }

    [Fact]
    public async Task HandleAsync_UserType_SucceedsWithoutPinValidation()
    {
        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.User, string.Empty));

        Assert.True(result.Succeeded);
        await _eventBus.Received(1).PublishAsync(Arg.Is<UserLoggedInEvent>(e =>
            e.UserType == UserType.User));
        await _loginService.DidNotReceive().ValidatePinAsync(Arg.Any<UserType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AdminNotFound_ReturnsFailure()
    {
        _loginService.ValidatePinAsync(UserType.Admin, "1111", Arg.Any<CancellationToken>())
            .Returns(LoginResult.Failed("User not found."));

        var result = await CreateSut().HandleAsync(new LoginUserCommand(UserType.Admin, "1111"));

        Assert.False(result.Succeeded);
        Assert.Equal("User not found.", result.ErrorMessage);
    }
}
