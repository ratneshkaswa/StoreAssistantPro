using System.Collections.ObjectModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Centralized notification management service. All notification
/// operations (add, mark read, clear) go through this service so
/// that domain events are published consistently and the bell icon
/// badge updates automatically.
/// <para>
/// <b>Architecture rules:</b>
/// <list type="bullet">
///   <item>ViewModels call <see cref="INotificationService"/> methods
///         — never <see cref="IAppStateService"/> notification methods
///         directly.</item>
///   <item>Other services (billing, connectivity, sales) call
///         <see cref="Post"/> to create notifications.</item>
///   <item>The service publishes <c>NotificationPostedEvent</c> after
///         every <see cref="Post"/> and <c>NotificationsChangedEvent</c>
///         after any operation that affects the unread count.</item>
/// </list>
/// </para>
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Live collection of all notifications. Bind to this from XAML
    /// for the notification popup list.
    /// </summary>
    ObservableCollection<AppNotification> Notifications { get; }

    /// <summary>
    /// Number of unread notifications. Updated automatically after
    /// every mutation.
    /// </summary>
    int UnreadCount { get; }

    /// <summary>
    /// Creates and stores a new notification with the given parameters.
    /// Publishes <c>NotificationPostedEvent</c> and
    /// <c>NotificationsChangedEvent</c>.
    /// </summary>
    Task PostAsync(string title, string message,
                   AppNotificationLevel level = AppNotificationLevel.Info);

    /// <summary>
    /// Marks a single notification as read.
    /// Publishes <c>NotificationsChangedEvent</c>.
    /// </summary>
    Task MarkReadAsync(AppNotification notification);

    /// <summary>
    /// Marks all unread notifications as read.
    /// Publishes <c>NotificationsChangedEvent</c>.
    /// </summary>
    Task MarkAllReadAsync();

    /// <summary>
    /// Removes all notifications from the store.
    /// Publishes <c>NotificationsChangedEvent</c>.
    /// </summary>
    Task ClearAsync();
}
