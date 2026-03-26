using System.Collections.ObjectModel;
using System.Windows.Threading;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton toast service. Manages an observable collection of
/// <see cref="ToastItem"/> instances with automatic timed removal.
/// <para>
/// Subscribes to <see cref="NotificationPostedEvent"/> so that every
/// notification posted via <see cref="INotificationService"/> also
/// spawns a brief visual toast — no extra caller code needed.
/// </para>
/// </summary>
public sealed class ToastService : IToastService, IDisposable
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);
    private static readonly int MaxVisibleToasts = 5;

    private readonly Dispatcher _dispatcher;
    private readonly IEventBus _eventBus;
    private readonly Dictionary<Guid, DispatcherTimer> _timers = [];

    public ObservableCollection<ToastItem> Toasts { get; } = [];

    public ToastService(IEventBus eventBus)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _eventBus = eventBus;

        _eventBus.Subscribe<NotificationPostedEvent>(OnNotificationPostedAsync);
    }

    public void Show(string message,
                     AppNotificationLevel level = AppNotificationLevel.Info,
                     TimeSpan? duration = null)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => Show(message, level, duration));
            return;
        }

        var toast = new ToastItem
        {
            Message = message,
            Level = level
        };

        Toasts.Add(toast);

        // Evict oldest when over the cap
        while (Toasts.Count > MaxVisibleToasts)
        {
            var oldest = Toasts[0];
            RemoveToast(oldest.Id);
        }

        // Start auto-dismiss timer
        var effectiveDuration = duration ?? DefaultDuration;
        var timer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = effectiveDuration
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            RemoveToast(toast.Id);
        };

        _timers[toast.Id] = timer;
        timer.Start();
    }

    public void Dismiss(Guid toastId)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => Dismiss(toastId));
            return;
        }

        RemoveToast(toastId);
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<NotificationPostedEvent>(OnNotificationPostedAsync);

        foreach (var timer in _timers.Values)
            timer.Stop();
        _timers.Clear();
    }

    // ── Private helpers ───────────────────────────────────────────

    private void RemoveToast(Guid id)
    {
        if (_timers.Remove(id, out var timer))
            timer.Stop();

        var item = Toasts.FirstOrDefault(t => t.Id == id);
        if (item is not null)
            Toasts.Remove(item);
    }

    private Task OnNotificationPostedAsync(NotificationPostedEvent e)
    {
        var n = e.Notification;
        var preferences = UserPreferencesStore.GetSnapshot();
        if (!preferences.InAppToastsEnabled || !UserPreferencesStore.MeetsNotificationThreshold(n.Level))
            return Task.CompletedTask;

        Show(n.Message, n.Level);
        return Task.CompletedTask;
    }
}
