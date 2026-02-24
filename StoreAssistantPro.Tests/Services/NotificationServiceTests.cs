using System.Collections.ObjectModel;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class NotificationServiceTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ObservableCollection<AppNotification> _notifications = [];

    private NotificationService CreateSut()
    {
        _appState.Notifications.Returns(_notifications);
        _appState.UnreadNotificationCount.Returns(_ => _notifications.Count(n => !n.IsRead));
        return new NotificationService(_appState, _eventBus);
    }

    // ── PostAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_AddsNotificationToAppState()
    {
        var sut = CreateSut();

        await sut.PostAsync("Title", "Message");

        _appState.Received(1).AddNotification(Arg.Is<AppNotification>(n =>
            n.Title == "Title" && n.Message == "Message" && n.Level == AppNotificationLevel.Info));
    }

    [Fact]
    public async Task PostAsync_WithLevel_SetsLevel()
    {
        var sut = CreateSut();

        await sut.PostAsync("Low Stock", "3 items", AppNotificationLevel.Warning);

        _appState.Received(1).AddNotification(Arg.Is<AppNotification>(n =>
            n.Level == AppNotificationLevel.Warning));
    }

    [Fact]
    public async Task PostAsync_PublishesNotificationPostedEvent()
    {
        var sut = CreateSut();

        await sut.PostAsync("Title", "Message");

        await _eventBus.Received(1).PublishAsync(Arg.Is<NotificationPostedEvent>(e =>
            e.Notification.Title == "Title"));
    }

    [Fact]
    public async Task PostAsync_PublishesNotificationsChangedEvent()
    {
        var sut = CreateSut();

        await sut.PostAsync("Title", "Message");

        await _eventBus.Received(1).PublishAsync(Arg.Any<NotificationsChangedEvent>());
    }

    // ── MarkReadAsync ──────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_DelegatesToAppState()
    {
        var sut = CreateSut();
        var notification = new AppNotification { Title = "T", Message = "M" };

        await sut.MarkReadAsync(notification);

        _appState.Received(1).MarkNotificationRead(notification);
    }

    [Fact]
    public async Task MarkReadAsync_PublishesChangedEvent()
    {
        var sut = CreateSut();
        var notification = new AppNotification { Title = "T", Message = "M" };

        await sut.MarkReadAsync(notification);

        await _eventBus.Received(1).PublishAsync(Arg.Any<NotificationsChangedEvent>());
    }

    // ── MarkAllReadAsync ───────────────────────────────────────────

    [Fact]
    public async Task MarkAllReadAsync_MarksEveryUnreadNotification()
    {
        var sut = CreateSut();
        var n1 = new AppNotification { Title = "A", Message = "M1" };
        var n2 = new AppNotification { Title = "B", Message = "M2", IsRead = true };
        var n3 = new AppNotification { Title = "C", Message = "M3" };
        _notifications.Add(n1);
        _notifications.Add(n2);
        _notifications.Add(n3);

        await sut.MarkAllReadAsync();

        _appState.Received(1).MarkNotificationRead(n1);
        _appState.DidNotReceive().MarkNotificationRead(n2);
        _appState.Received(1).MarkNotificationRead(n3);
    }

    [Fact]
    public async Task MarkAllReadAsync_PublishesChangedEvent()
    {
        var sut = CreateSut();

        await sut.MarkAllReadAsync();

        await _eventBus.Received(1).PublishAsync(Arg.Any<NotificationsChangedEvent>());
    }

    // ── ClearAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ClearAsync_DelegatesToAppState()
    {
        var sut = CreateSut();

        await sut.ClearAsync();

        _appState.Received(1).ClearNotifications();
    }

    [Fact]
    public async Task ClearAsync_PublishesChangedEventWithZeroCount()
    {
        var sut = CreateSut();

        await sut.ClearAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<NotificationsChangedEvent>(e => e.UnreadCount == 0));
    }

    // ── Notifications property ─────────────────────────────────────

    [Fact]
    public void Notifications_DelegatesToAppState()
    {
        var sut = CreateSut();

        Assert.Same(_notifications, sut.Notifications);
    }

    // ── UnreadCount property ───────────────────────────────────────

    [Fact]
    public void UnreadCount_DelegatesToAppState()
    {
        var sut = CreateSut();
        var n = new AppNotification { Title = "T", Message = "M" };
        _notifications.Add(n);

        Assert.Equal(1, sut.UnreadCount);
    }
}
