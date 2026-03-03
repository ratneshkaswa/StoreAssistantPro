using System.Collections.Concurrent;
using System.Reflection;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Lightweight in-process event bus. Thread-safe, sequential dispatch
/// on the caller's context (suitable for WPF single-threaded UI).
/// <para>
/// <b>Weak subscriptions:</b> Instance-method handlers are stored via
/// <see cref="WeakReference"/> so the EventBus (a singleton) never
/// prevents a ViewModel from being garbage-collected. Dead references
/// are automatically pruned during <see cref="PublishAsync{TEvent}"/>.
/// Static or closure handlers are stored with a strong reference
/// (they have no target to prevent GC of).
/// </para>
/// <para>
/// <b>Fault isolation:</b> A throwing handler is caught and silently
/// skipped so remaining subscribers still receive the event.
/// </para>
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Subscription>> _subscriptions = [];
    private readonly Lock _lock = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var key = typeof(TEvent);
        lock (_lock)
        {
            var list = _subscriptions.GetOrAdd(key, _ => []);
            list.Add(Subscription.Create(handler));
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var key = typeof(TEvent);
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(key, out var list))
                return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Matches(handler))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var key = typeof(TEvent);

        Subscription[] snapshot;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(key, out var list) || list.Count == 0)
                return;

            // Prune dead weak references while building the snapshot.
            list.RemoveAll(s => !s.IsAlive);
            snapshot = [.. list];
        }

        foreach (var sub in snapshot)
        {
            if (sub.TryInvoke<TEvent>(@event, out var task))
            {
                try
                {
                    await task;
                }
                catch
                {
                    // Fault isolation — a handler that returns a faulted
                    // Task must not prevent remaining subscribers from
                    // receiving the event, nor surface as an unobserved
                    // Task exception when callers fire-and-forget.
                }
            }
        }
    }

    // ── Subscription wrapper ───────────────────────────────────────

    /// <summary>
    /// Wraps a handler delegate. Instance methods are stored as a
    /// <see cref="WeakReference"/> + <see cref="MethodInfo"/> pair so
    /// the EventBus does not prevent the target from being GC'd.
    /// Static / closure-captured handlers use a strong reference.
    /// </summary>
    private sealed class Subscription
    {
        // Weak path (instance methods)
        private readonly WeakReference? _targetRef;
        private readonly MethodInfo? _method;

        // Strong path (static methods, closures)
        private readonly Delegate? _strongHandler;

        private Subscription(WeakReference targetRef, MethodInfo method)
        {
            _targetRef = targetRef;
            _method = method;
        }

        private Subscription(Delegate strongHandler)
        {
            _strongHandler = strongHandler;
        }

        public static Subscription Create<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
        {
            if (handler.Target is not null)
                return new Subscription(new WeakReference(handler.Target), handler.Method);

            return new Subscription(handler);
        }

        /// <summary>
        /// Returns <c>false</c> when the weak target has been collected.
        /// Always <c>true</c> for strong (static/closure) handlers.
        /// </summary>
        public bool IsAlive =>
            _strongHandler is not null || (_targetRef is not null && _targetRef.IsAlive);

        /// <summary>
        /// Attempts to invoke the handler. Returns <c>false</c> if the
        /// weak target was collected or invocation fails.
        /// </summary>
        public bool TryInvoke<TEvent>(TEvent @event, out Task task) where TEvent : IEvent
        {
            try
            {
                if (_strongHandler is Func<TEvent, Task> strong)
                {
                    task = strong(@event);
                    return true;
                }

                if (_targetRef is not null && _targetRef.Target is { } target)
                {
                    // Create a bound delegate from the stored MethodInfo + live target.
                    // This avoids the object[] allocation of MethodInfo.Invoke and
                    // is faster than DynamicInvoke for repeated calls.
                    var typedDelegate = (Func<TEvent, Task>)Delegate.CreateDelegate(
                        typeof(Func<TEvent, Task>), target, _method!);
                    task = typedDelegate(@event);
                    return true;
                }
            }
            catch
            {
                // Fault isolation — do not let one handler kill dispatch.
            }

            task = Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// Checks whether this subscription was created from the same
        /// delegate, enabling explicit <see cref="EventBus.Unsubscribe{TEvent}"/>.
        /// </summary>
        public bool Matches(Delegate handler)
        {
            if (_strongHandler is not null)
                return _strongHandler.Equals(handler);

            return _targetRef?.Target is not null
                && ReferenceEquals(_targetRef.Target, handler.Target)
                && _method == handler.Method;
        }
    }
}
