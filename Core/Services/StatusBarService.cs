using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// <see cref="IStatusBarService"/> implementation that uses a
/// <see cref="DispatcherTimer"/> for auto-clear. Runs on the UI
/// thread and marshals background updates back to the app dispatcher.
/// </summary>
public partial class StatusBarService : ObservableObject, IStatusBarService, IDisposable
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

    private readonly Dispatcher? _dispatcher;
    private readonly DispatcherTimer _clearTimer;
    private bool _disposed;

    public StatusBarService(Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? Application.Current?.Dispatcher;
        _clearTimer = _dispatcher is not null
            ? new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
            : new DispatcherTimer();
        _clearTimer.Tick += OnClearTimerTick;

        DefaultMessage = "Ready";
        Message = DefaultMessage;
    }

    // ── Observable state ─────────────────────────────────────────────

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string DefaultMessage { get; set; }

    // ── Public API ───────────────────────────────────────────────────

    public void Post(string message, TimeSpan duration)
    {
        RunOnDispatcher(() =>
        {
            _clearTimer.Stop();
            Message = message;
            _clearTimer.Interval = duration;
            _clearTimer.Start();
        });
    }

    public void Post(string message) => Post(message, DefaultDuration);

    public void SetPersistent(string message)
    {
        RunOnDispatcher(() =>
        {
            _clearTimer.Stop();
            Message = message;
        });
    }

    public void Clear()
    {
        RunOnDispatcher(() =>
        {
            _clearTimer.Stop();
            Message = DefaultMessage;
        });
    }

    // ── Timer callback ───────────────────────────────────────────────

    private void OnClearTimerTick(object? sender, EventArgs e)
    {
        _clearTimer.Stop();
        Message = DefaultMessage;
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcher is null
            || _dispatcher.HasShutdownStarted
            || _dispatcher.HasShutdownFinished)
        {
            action();
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        RunOnDispatcher(() =>
        {
            _clearTimer.Stop();
            _clearTimer.Tick -= OnClearTimerTick;
        });

        GC.SuppressFinalize(this);
    }
}
