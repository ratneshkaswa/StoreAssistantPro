using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Periodically pings the database with a lightweight
/// <c>SELECT 1</c> and publishes connectivity events when the
/// state changes.
/// <para>
/// <b>Health check strategy:</b> Uses
/// <see cref="AppDbContext.Database"/>.<see cref="DatabaseFacade.CanConnectAsync"/>
/// which opens a connection, sends a trivial command, and closes it.
/// No table locks, no schema dependency, minimal overhead.
/// </para>
/// <para>
/// <b>Interval:</b> Defaults to 30 seconds. A shorter interval can
/// be injected via the test-friendly constructor.
/// </para>
/// </summary>
public sealed class ConnectivityMonitorService : IConnectivityMonitorService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConnectivityMonitorService> _logger;
    private readonly Lock _lock = new();

    private Timer? _timer;
    private readonly SemaphoreSlim _checkGate = new(1, 1);
    private bool _isConnected = true;
    private Stopwatch? _downtimeStopwatch;
    private bool _started;

    /// <summary>Default: 30 seconds.</summary>
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    public ConnectivityMonitorService(
        IDbContextFactory<AppDbContext> contextFactory,
        IEventBus eventBus,
        ILogger<ConnectivityMonitorService> logger)
        : this(contextFactory, eventBus, logger, DefaultInterval)
    {
    }

    /// <summary>
    /// Test-friendly constructor with explicit poll interval.
    /// Pass <see cref="Timeout.InfiniteTimeSpan"/> as
    /// <paramref name="pollInterval"/> to disable the background timer
    /// entirely (useful for unit tests that call
    /// <see cref="CheckNowAsync"/> directly).
    /// </summary>
    public ConnectivityMonitorService(
        IDbContextFactory<AppDbContext> contextFactory,
        IEventBus eventBus,
        ILogger<ConnectivityMonitorService> logger,
        TimeSpan pollInterval)
    {
        _contextFactory = contextFactory;
        _eventBus = eventBus;
        _logger = logger;
        PollInterval = pollInterval;
    }

    // ── Public state ───────────────────────────────────────────────

    public TimeSpan PollInterval { get; }

    public bool IsConnected
    {
        get { lock (_lock) return _isConnected; }
    }

    // ── Lifecycle ──────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_started) return;
            _started = true;
        }

        // Initial check — establishes baseline state before the
        // first timer tick.
        await CheckNowAsync(ct).ConfigureAwait(false);

        if (PollInterval != Timeout.InfiniteTimeSpan)
        {
            _timer = new Timer(
                OnTimerElapsed,
                state: null,
                dueTime: PollInterval,
                period: PollInterval);
        }
    }

    /// <summary>
    /// Performs a single connectivity check and publishes an event if
    /// the state changed. Called by the timer callback and also
    /// available for on-demand checks (e.g. before a critical write).
    /// </summary>
    public async Task CheckNowAsync(CancellationToken ct = default)
    {
        await _checkGate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            var reachable = await PingDatabaseAsync(ct).ConfigureAwait(false);

            bool wasConnected;
            var downtime = TimeSpan.Zero;

            lock (_lock)
            {
                wasConnected = _isConnected;

                if (wasConnected && !reachable)
                {
                    _isConnected = false;
                    _downtimeStopwatch = Stopwatch.StartNew();
                }
                else if (!wasConnected && reachable)
                {
                    _isConnected = true;
                    downtime = _downtimeStopwatch?.Elapsed ?? TimeSpan.Zero;
                    _downtimeStopwatch = null;
                }
                else
                {
                    // No state change — nothing to publish.
                    return;
                }
            }

            // Publish events only on transitions.
            if (wasConnected && !reachable)
            {
                _logger.LogWarning("Database connectivity lost");
                await PublishSafeAsync(new ConnectionLostEvent()).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation(
                    "Database connectivity restored after {Downtime}", downtime);
                await PublishSafeAsync(new ConnectionRestoredEvent(downtime))
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _checkGate.Release();
        }
    }

    // ── Internals ──────────────────────────────────────────────────

    private async Task<bool> PingDatabaseAsync(CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory
                .CreateDbContextAsync(ct).ConfigureAwait(false);
            return await context.Database.CanConnectAsync(ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw; // Respect cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database ping failed");
            return false;
        }
    }

    private async void OnTimerElapsed(object? state)
    {
        try
        {
            if (!await _checkGate.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
                return;

            try
            {
                await CheckNowCoreAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                _checkGate.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connectivity check failed unexpectedly");
        }
    }

    private async Task PublishSafeAsync<TEvent>(TEvent @event)
        where TEvent : IEvent
    {
        try
        {
            await _eventBus.PublishAsync(@event).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish {EventType} — event swallowed",
                typeof(TEvent).Name);
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        // Change timer to never fire again, then dispose.
        // This prevents a race where the callback fires after Dispose.
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
        _checkGate.Dispose();
    }

    private async Task CheckNowCoreAsync(CancellationToken ct)
    {
        var reachable = await PingDatabaseAsync(ct).ConfigureAwait(false);

        bool wasConnected;
        var downtime = TimeSpan.Zero;

        lock (_lock)
        {
            wasConnected = _isConnected;

            if (wasConnected && !reachable)
            {
                _isConnected = false;
                _downtimeStopwatch = Stopwatch.StartNew();
            }
            else if (!wasConnected && reachable)
            {
                _isConnected = true;
                downtime = _downtimeStopwatch?.Elapsed ?? TimeSpan.Zero;
                _downtimeStopwatch = null;
            }
            else
            {
                // No state change — nothing to publish.
                return;
            }
        }

        // Publish events only on transitions.
        if (wasConnected && !reachable)
        {
            _logger.LogWarning("Database connectivity lost");
            await PublishSafeAsync(new ConnectionLostEvent()).ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation(
                "Database connectivity restored after {Downtime}", downtime);
            await PublishSafeAsync(new ConnectionRestoredEvent(downtime))
                .ConfigureAwait(false);
        }
    }
}
