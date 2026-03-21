using System.Windows.Threading;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Monitors user inactivity via a DispatcherTimer and raises
/// <see cref="InactivityTimeout"/> when the configured idle period expires (#462).
/// </summary>
public sealed class AutoLogoutService : IAutoLogoutService, IDisposable
{
    private readonly DispatcherTimer _timer;
    private int _timeoutMinutes;

    public AutoLogoutService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += OnTimerTick;
    }

    public int TimeoutMinutes => _timeoutMinutes;

    private int _idleMinutes;

    public event EventHandler? InactivityTimeout;

    public void Configure(int timeoutMinutes)
    {
        _timeoutMinutes = Math.Max(0, timeoutMinutes);
        _idleMinutes = 0;

        if (_timeoutMinutes == 0)
            Stop();
    }

    public void ResetTimer() => _idleMinutes = 0;

    public void Start()
    {
        if (_timeoutMinutes <= 0)
            return;

        _idleMinutes = 0;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _idleMinutes = 0;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _idleMinutes++;

        if (_timeoutMinutes > 0 && _idleMinutes >= _timeoutMinutes)
        {
            Stop();
            InactivityTimeout?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
