using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
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
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    public ChangePinHandlerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));
    }

    private ChangePinHandler CreateSut() => new(_userService, _loginService, _contextFactory, _appState, _eventBus);

    [Fact]
    public async Task HandleAsync_UserPin_SucceedsWithoutMasterPin()
    {
        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.User, "1234", null));

        Assert.True(result.Succeeded);
        await _userService.Received(1).ChangePinAsync(UserType.User, "1234", Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<PinChangedEvent>(e =>
            e.UserType == UserType.User));
    }

    [Fact]
    public async Task HandleAsync_AdminPin_RequiresMasterPin()
    {
        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "1234", ""));

        Assert.False(result.Succeeded);
        Assert.Contains("Master PIN is required", result.ErrorMessage);
        await _userService.DidNotReceive().ChangePinAsync(Arg.Any<UserType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AdminPin_InvalidMaster_Fails()
    {
        _loginService.ValidateMasterPinAsync("000000", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "1234", "000000"));

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid Master PIN", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_AdminPin_ValidMaster_Succeeds()
    {
        _loginService.ValidateMasterPinAsync("123456", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().HandleAsync(
            new ChangePinCommand(UserType.Admin, "9999", "123456"));

        Assert.True(result.Succeeded);
        await _userService.Received(1).ChangePinAsync(UserType.Admin, "9999", Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<PinChangedEvent>(e =>
            e.UserType == UserType.Admin));
    }
}
