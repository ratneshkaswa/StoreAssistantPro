using System.Collections.Concurrent;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Lightweight in-process event bus. Thread-safe, synchronous dispatch
/// on the caller's context (suitable for WPF single-threaded UI).
/// <para>
/// Subscribers are stored by event type. Call <see cref="Unsubscribe{TEvent}"/>
/// in <c>Dispose</c> to prevent stale references.
/// </para>
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = [];
    private readonly object _lock = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var key = typeof(TEvent);
        lock (_lock)
        {
            var list = _handlers.GetOrAdd(key, _ => []);
            list.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var key = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.TryGetValue(key, out var list))
                list.Remove(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var key = typeof(TEvent);

        Delegate[] snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(key, out var list) || list.Count == 0)
                return;

            snapshot = [.. list];
        }

        foreach (var handler in snapshot)
        {
            if (handler is Func<TEvent, Task> typed)
                await typed(@event);
        }
    }
}
