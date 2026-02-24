using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published after a new <see cref="AppNotification"/> is added to the
/// notification store. Subscribers can use this to show toasts, play
/// sounds, or trigger other transient UI effects.
/// </summary>
public sealed record NotificationPostedEvent(AppNotification Notification) : IEvent;
