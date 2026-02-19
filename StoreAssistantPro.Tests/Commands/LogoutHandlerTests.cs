using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Tests.Commands;

public class LogoutHandlerTests
{
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private LogoutHandler CreateSut() => new(_sessionService, _appState, _eventBus);

    [Fact]
    public async Task HandleAsync_ClearsBillingSession()
    {
        var result = await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        Assert.True(result.Succeeded);
        _appState.Received(1).SetBillingSession(null);
    }

    [Fact]
    public async Task HandleAsync_CallsSessionLogout()
    {
        await CreateSut().HandleAsync(new LogoutCommand(UserType.Manager));

        _sessionService.Received(1).Logout();
    }

    [Fact]
    public async Task HandleAsync_ClearsNotifications()
    {
        await CreateSut().HandleAsync(new LogoutCommand(UserType.User));

        _appState.Received(1).ClearNotifications();
    }

    [Fact]
    public async Task HandleAsync_PublishesUserLoggedOutEvent()
    {
        await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        await _eventBus.Received(1).PublishAsync(Arg.Is<UserLoggedOutEvent>(e =>
            e.UserType == UserType.Admin));
    }

    [Fact]
    public async Task HandleAsync_ExecutesInCorrectOrder()
    {
        var order = new List<string>();
        _appState.When(x => x.SetBillingSession(null)).Do(_ => order.Add("billing"));
        _sessionService.When(x => x.Logout()).Do(_ => order.Add("session"));
        _appState.When(x => x.ClearNotifications()).Do(_ => order.Add("notifications"));
        _eventBus.PublishAsync(Arg.Any<UserLoggedOutEvent>())
            .Returns(ci => { order.Add("event"); return Task.CompletedTask; });

        await CreateSut().HandleAsync(new LogoutCommand(UserType.Admin));

        Assert.Equal(["billing", "session", "notifications", "event"], order);
    }
}
