namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Dispatches events to subscribers. Singleton — lives for the
/// entire app lifetime. Subscribers are weak-referenced to prevent
/// memory leaks when ViewModels are garbage-collected.
/// </summary>
public interface IEventBus
{
    /// <summary>Register a handler to be called when <typeparamref name="TEvent"/> is published.</summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>Remove a previously registered handler.</summary>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;

    /// <summary>Publish an event to all current subscribers of <typeparamref name="TEvent"/>.</summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
}
