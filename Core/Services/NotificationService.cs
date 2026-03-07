using System.Collections.ObjectModel;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton notification service. Delegates storage to
/// <see cref="IAppStateService"/> and publishes domain events via
/// <see cref="IEventBus"/> so the bell icon badge, status bar counter,
/// and any future toast/sound systems update automatically.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly IRegionalSettingsService _regional;

    public NotificationService(IAppStateService appState, IEventBus eventBus, IRegionalSettingsService regional)
    {
        _appState = appState;
        _eventBus = eventBus;
        _regional = regional;
    }

    public ObservableCollection<AppNotification> Notifications => _appState.Notifications;

    public int UnreadCount => _appState.UnreadNotificationCount;

    public async Task PostAsync(string title, string message,
                                AppNotificationLevel level = AppNotificationLevel.Info)
    {
        var notification = new AppNotification
        {
            Title = title,
            Message = message,
            Level = level,
            Timestamp = _regional.Now
        };

        _appState.AddNotification(notification);

        await _eventBus.PublishAsync(new NotificationPostedEvent(notification));
        await _eventBus.PublishAsync(new NotificationsChangedEvent(UnreadCount));
    }

    public async Task MarkReadAsync(AppNotification notification)
    {
        _appState.MarkNotificationRead(notification);

        await _eventBus.PublishAsync(new NotificationsChangedEvent(UnreadCount));
    }

    public async Task MarkAllReadAsync()
    {
        foreach (var n in _appState.Notifications.Where(n => !n.IsRead).ToList())
            _appState.MarkNotificationRead(n);

        await _eventBus.PublishAsync(new NotificationsChangedEvent(UnreadCount));
    }

    public async Task ClearAsync()
    {
        _appState.ClearNotifications();

        await _eventBus.PublishAsync(new NotificationsChangedEvent(0));
    }
}
