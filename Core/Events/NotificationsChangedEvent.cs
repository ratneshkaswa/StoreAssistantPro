namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when the notification list changes in a way that affects
/// the unread count — after marking items read or clearing all
/// notifications. Subscribers use this to refresh badge counts
/// and summary displays.
/// </summary>
public sealed record NotificationsChangedEvent(int UnreadCount) : IEvent;
