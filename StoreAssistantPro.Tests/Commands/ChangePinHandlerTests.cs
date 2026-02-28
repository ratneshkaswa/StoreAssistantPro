using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Events;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Tests.Commands;

public class ChangePinHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private ChangePinHandler CreateSut() => new(_userService, _loginService, _eventBus);

    [Fact]
    public async Task HandleAsync_ManagerPin_SucceedsWithoutMasterPin()
    {
        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Manager, "1234", null));

        Assert.True(result.Succeeded);
        await _userService.Received(1).ChangePinAsync(UserType.Manager, "1234");
        await _eventBus.Received(1).PublishAsync(Arg.Is<PinChangedEvent>(e =>
            e.UserType == UserType.Manager));
    }

    [Fact]
    public async Task HandleAsync_AdminPin_RequiresMasterPin()
    {
        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "1234", ""));

        Assert.False(result.Succeeded);
        Assert.Contains("Master Password is required", result.ErrorMessage);
        await _userService.DidNotReceive().ChangePinAsync(Arg.Any<UserType>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_AdminPin_InvalidMaster_Fails()
    {
        _loginService.ValidateMasterPinAsync("000000").Returns(false);

        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "1234", "000000"));

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid Master Password", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_AdminPin_ValidMaster_Succeeds()
    {
        _loginService.ValidateMasterPinAsync("123456").Returns(true);

        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "9999", "123456"));

        Assert.True(result.Succeeded);
        await _userService.Received(1).ChangePinAsync(UserType.Admin, "9999");
        await _eventBus.Received(1).PublishAsync(Arg.Is<PinChangedEvent>(e =>
            e.UserType == UserType.Admin));
    }
}
