using Microsoft.Extensions.Logging;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

[Collection("UserPreferences")]
public sealed class WindowsNotificationBridgeTests : IDisposable
{
    public WindowsNotificationBridgeTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public void Constructor_Should_Register_Presenter_And_Subscribe_To_NotificationEvents()
    {
        var eventBus = Substitute.For<IEventBus>();
        var presenter = Substitute.For<IWindowsNotificationPresenter>();
        var logger = Substitute.For<ILogger<WindowsNotificationBridge>>();

        _ = new WindowsNotificationBridge(eventBus, presenter, logger);

        presenter.Received(1).EnsureRegistered();
        eventBus.Received(1).Subscribe<NotificationPostedEvent>(Arg.Any<Func<NotificationPostedEvent, Task>>());
    }

    [Fact]
    public async Task NotificationPostedEvent_Should_Be_Forwarded_To_WindowsPresenter()
    {
        var eventBus = Substitute.For<IEventBus>();
        var presenter = Substitute.For<IWindowsNotificationPresenter>();
        var logger = Substitute.For<ILogger<WindowsNotificationBridge>>();
        Func<NotificationPostedEvent, Task>? handler = null;

        eventBus
            .When(x => x.Subscribe<NotificationPostedEvent>(Arg.Any<Func<NotificationPostedEvent, Task>>()))
            .Do(callInfo => handler = callInfo.Arg<Func<NotificationPostedEvent, Task>>());

        _ = new WindowsNotificationBridge(eventBus, presenter, logger);

        var notification = new AppNotification
        {
            Title = "Stock alert",
            Message = "2 items are low",
            Timestamp = new DateTime(2026, 3, 25, 10, 30, 0),
            Level = AppNotificationLevel.Warning
        };

        Assert.NotNull(handler);
        await handler!(new NotificationPostedEvent(notification));

        presenter.Received(1).TryShow(notification);
    }

    [Fact]
    public async Task NotificationPostedEvent_Should_Respect_User_Preferences()
    {
        var eventBus = Substitute.For<IEventBus>();
        var presenter = Substitute.For<IWindowsNotificationPresenter>();
        var logger = Substitute.For<ILogger<WindowsNotificationBridge>>();
        Func<NotificationPostedEvent, Task>? handler = null;

        eventBus
            .When(x => x.Subscribe<NotificationPostedEvent>(Arg.Any<Func<NotificationPostedEvent, Task>>()))
            .Do(callInfo => handler = callInfo.Arg<Func<NotificationPostedEvent, Task>>());

        UserPreferencesStore.Update(state =>
        {
            state.WindowsNotificationsEnabled = false;
            state.MinimumNotificationLevel = AppNotificationLevel.Warning;
        });

        _ = new WindowsNotificationBridge(eventBus, presenter, logger);

        Assert.NotNull(handler);
        await handler!(new NotificationPostedEvent(new AppNotification
        {
            Title = "Tip",
            Message = "Informational event",
            Timestamp = DateTime.UtcNow,
            Level = AppNotificationLevel.Info
        }));

        presenter.DidNotReceive().TryShow(Arg.Any<AppNotification>());
    }

    [Fact]
    public void Dispose_Should_Unsubscribe_From_NotificationEvents()
    {
        var eventBus = Substitute.For<IEventBus>();
        var presenter = Substitute.For<IWindowsNotificationPresenter>();
        var logger = Substitute.For<ILogger<WindowsNotificationBridge>>();
        var sut = new WindowsNotificationBridge(eventBus, presenter, logger);

        sut.Dispose();

        eventBus.Received(1).Unsubscribe<NotificationPostedEvent>(Arg.Any<Func<NotificationPostedEvent, Task>>());
    }
}
