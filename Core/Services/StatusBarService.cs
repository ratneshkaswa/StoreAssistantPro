using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// <see cref="IStatusBarService"/> implementation that uses a
/// <see cref="DispatcherTimer"/> for auto-clear.  Runs on the UI
/// thread — safe for direct property-change notifications.
/// </summary>
public partial class StatusBarService : ObservableObject, IStatusBarService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

    private readonly DispatcherTimer _clearTimer;

    public StatusBarService()
    {
        _clearTimer = new DispatcherTimer();
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
        _clearTimer.Stop();
        Message = message;
        _clearTimer.Interval = duration;
        _clearTimer.Start();
    }

    public void Post(string message) => Post(message, DefaultDuration);

    public void SetPersistent(string message)
    {
        _clearTimer.Stop();
        Message = message;
    }

    public void Clear()
    {
        _clearTimer.Stop();
        Message = DefaultMessage;
    }

    // ── Timer callback ───────────────────────────────────────────────

    private void OnClearTimerTick(object? sender, EventArgs e)
    {
        _clearTimer.Stop();
        Message = DefaultMessage;
    }
}
