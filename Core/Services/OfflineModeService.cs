using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Reacts to <see cref="ConnectionLostEvent"/> and
/// <see cref="ConnectionRestoredEvent"/> from
/// <see cref="IConnectivityMonitorService"/> and coordinates the
/// offline/online mode transition across the application.
/// <para>
/// <b>Flow:</b>
/// <code>
/// ConnectivityMonitorService
///   ├─ publishes ConnectionLostEvent
///   └─ publishes ConnectionRestoredEvent
///         │
///         ▼
/// OfflineModeService (this)
///   ├─ AppState.SetConnectivity(…)
///   ├─ StatusBar.SetPersistent / Post
///   └─ publishes OfflineModeChangedEvent
/// </code>
/// </para>
/// </summary>
public sealed class OfflineModeService : IOfflineModeService
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly IStatusBarService _statusBar;
    private readonly IRegionalSettingsService _regional;
    private readonly ILogger<OfflineModeService> _logger;
    private readonly Lock _lock = new();

    private bool _isOffline;

    public OfflineModeService(
        IAppStateService appState,
        IEventBus eventBus,
        IStatusBarService statusBar,
        IRegionalSettingsService regional,
        ILogger<OfflineModeService> logger)
    {
        _appState = appState;
        _eventBus = eventBus;
        _statusBar = statusBar;
        _regional = regional;
        _logger = logger;

        _eventBus.Subscribe<ConnectionLostEvent>(OnConnectionLostAsync);
        _eventBus.Subscribe<ConnectionRestoredEvent>(OnConnectionRestoredAsync);
    }

    // ── Public state ───────────────────────────────────────────────

    public bool IsOffline
    {
        get { lock (_lock) return _isOffline; }
    }

    // ── Event handlers ─────────────────────────────────────────────

    private async Task OnConnectionLostAsync(ConnectionLostEvent _)
    {
        lock (_lock)
        {
            if (_isOffline) return;
            _isOffline = true;
        }

        _logger.LogWarning("Entering offline mode");

        _appState.SetConnectivity(isOffline: true, _regional.Now);
        _statusBar.SetPersistent("⚠ OFFLINE — Database unreachable");

        await PublishSafeAsync(
            new OfflineModeChangedEvent(IsOffline: true, TimeSpan.Zero))
            .ConfigureAwait(false);
    }

    private async Task OnConnectionRestoredAsync(ConnectionRestoredEvent e)
    {
        lock (_lock)
        {
            if (!_isOffline) return;
            _isOffline = false;
        }

        _logger.LogInformation(
            "Exiting offline mode after {Downtime}", e.DowntimeDuration);

        _appState.SetConnectivity(isOffline: false, _regional.Now);
        _statusBar.Post("✅ Connection restored");

        await PublishSafeAsync(
            new OfflineModeChangedEvent(IsOffline: false, e.DowntimeDuration))
            .ConfigureAwait(false);
    }

    // ── Internals ──────────────────────────────────────────────────

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
        _eventBus.Unsubscribe<ConnectionLostEvent>(OnConnectionLostAsync);
        _eventBus.Unsubscribe<ConnectionRestoredEvent>(OnConnectionRestoredAsync);
    }
}
