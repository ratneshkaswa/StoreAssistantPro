namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Dispatches domain events to subscribers. Registered as a singleton —
/// lives for the entire application lifetime.
/// <para>
/// <b>Weak subscriptions:</b> Instance-method handlers are held via
/// <see cref="WeakReference"/> so the bus never prevents a ViewModel
/// (or any subscriber) from being garbage-collected. Dead references
/// are pruned automatically during <see cref="PublishAsync{TEvent}"/>.
/// </para>
/// <para>
/// <b>Explicit unsubscribe</b> is still recommended in <c>Dispose</c>
/// for deterministic cleanup and to avoid stale invocations between
/// GC cycles.
/// </para>
/// </summary>
public interface IEventBus
{
    /// <summary>Register a handler to be called when <typeparamref name="TEvent"/> is published.</summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>Remove a previously registered handler.</summary>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>Publish an event to all live subscribers of <typeparamref name="TEvent"/>.</summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
}
