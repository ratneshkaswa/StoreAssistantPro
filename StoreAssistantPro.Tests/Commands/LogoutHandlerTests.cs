using Microsoft.Extensions.Logging;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Tests.Commands;

public class LogoutHandlerTests
{
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ILogger<LogoutHandler> _logger = Substitute.For<ILogger<LogoutHandler>>();

    private LogoutHandler CreateSut() => new(_sessionService, _eventBus, _logger);

    [Fact]
    public async Task HandleAsync_ReturnsSuccess()
    {
        var result = await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task HandleAsync_CallsSessionLogout()
    {
        await CreateSut().HandleAsync(new LogoutCommand(UserType.User));

        _sessionService.Received(1).Logout();
    }

    [Fact]
    public async Task HandleAsync_PublishesUserLoggedOutEvent()
    {
        await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        await _eventBus.Received(1).PublishAsync(Arg.Is<UserLoggedOutEvent>(e =>
            e.UserType == UserType.Admin));
    }

    [Fact]
    public async Task HandleAsync_LogoutBeforeEvent()
    {
        var order = new List<string>();
        _sessionService.When(x => x.Logout()).Do(_ => order.Add("logout"));
        _eventBus.PublishAsync(Arg.Any<UserLoggedOutEvent>())
            .Returns(ci => { order.Add("event"); return Task.CompletedTask; });

        await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        Assert.Equal(["logout", "event"], order);
    }
}
