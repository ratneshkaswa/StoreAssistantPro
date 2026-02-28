using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Listens to billing lifecycle and cart-change events and persists
/// the cart state with a debounce strategy that avoids excessive writes
/// while guaranteeing data safety at critical moments.
/// <para>
/// <b>Debounce behavior:</b>
/// </para>
/// <list type="bullet">
///   <item>Each <see cref="CartChangedEvent"/> resets a timer. When the
///         timer fires (after <see cref="DebounceDelay"/>), the latest
///         snapshot is written to the database.</item>
///   <item><see cref="PaymentStartedEvent"/> immediately flushes the
///         pending snapshot — no debounce wait.</item>
///   <item><see cref="BillingSessionCompletedEvent"/> and
///         <see cref="BillingSessionCancelledEvent"/> cancel any
///         pending timer and mark the row inactive.</item>
/// </list>
/// <para>
/// All DB writes happen on a background thread via
/// <see cref="IBillingSessionPersistenceService"/>. The UI thread
/// is never blocked.
/// </para>
/// </summary>
public sealed class BillingAutoSaveService : IBillingAutoSaveService
{
    private readonly IBillingSessionPersistenceService _persistence;
    private readonly IEventBus _eventBus;
    private readonly ILogger<BillingAutoSaveService> _logger;
    private readonly Lock _lock = new();

    private Timer? _debounceTimer;
    private Guid _currentSessionId;
    private string? _pendingData;
    private bool _isSaving;

    public BillingAutoSaveService(
        IBillingSessionPersistenceService persistence,
        IEventBus eventBus,
        ILogger<BillingAutoSaveService> logger)
        : this(persistence, eventBus, logger, TimeSpan.FromSeconds(1))
    {
    }

    /// <summary>
    /// Overload that accepts a custom debounce delay. Used by tests to
    /// shorten the timer for deterministic assertions.
    /// </summary>
    public BillingAutoSaveService(
        IBillingSessionPersistenceService persistence,
        IEventBus eventBus,
        ILogger<BillingAutoSaveService> logger,
        TimeSpan debounceDelay)
    {
        _persistence = persistence;
        _eventBus = eventBus;
        _logger = logger;
        DebounceDelay = debounceDelay;

        _eventBus.Subscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Subscribe<CartChangedEvent>(OnCartChangedAsync);
        _eventBus.Subscribe<PaymentStartedEvent>(OnPaymentStartedAsync);
        _eventBus.Subscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Subscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);
    }

    // ── Public state ───────────────────────────────────────────────

    public TimeSpan DebounceDelay { get; }

    public bool IsSaving
    {
        get { lock (_lock) return _isSaving; }
    }

    // ── Event handlers ─────────────────────────────────────────────

    private Task OnSessionStartedAsync(BillingSessionStartedEvent _)
    {
        lock (_lock)
        {
            CancelTimerLocked();
            _pendingData = null;
        }

        return Task.CompletedTask;
    }

    private Task OnCartChangedAsync(CartChangedEvent e)
    {
        lock (_lock)
        {
            _currentSessionId = e.SessionId;
            _pendingData = e.SerializedBillData;
            RestartTimerLocked();
        }

        return Task.CompletedTask;
    }

    private async Task OnPaymentStartedAsync(PaymentStartedEvent e)
    {
        string dataToSave;

        lock (_lock)
        {
            CancelTimerLocked();
            _currentSessionId = e.SessionId;
            _pendingData = e.SerializedBillData;
            dataToSave = e.SerializedBillData;
        }

        await SaveNowAsync(e.SessionId, dataToSave).ConfigureAwait(false);
    }

    private async Task OnSessionCompletedAsync(BillingSessionCompletedEvent _)
    {
        Guid sessionId;

        lock (_lock)
        {
            CancelTimerLocked();
            sessionId = _currentSessionId;
            _pendingData = null;
        }

        if (sessionId != Guid.Empty)
            await MarkSessionInactiveAsync(sessionId, completed: true).ConfigureAwait(false);
    }

    private async Task OnSessionCancelledAsync(BillingSessionCancelledEvent _)
    {
        Guid sessionId;

        lock (_lock)
        {
            CancelTimerLocked();
            sessionId = _currentSessionId;
            _pendingData = null;
        }

        if (sessionId != Guid.Empty)
            await MarkSessionInactiveAsync(sessionId, completed: false).ConfigureAwait(false);
    }

    // ── Debounce timer ─────────────────────────────────────────────

    /// <summary>Resets the debounce timer. Must be called under <see cref="_lock"/>.</summary>
    private void RestartTimerLocked()
    {
        _debounceTimer?.Change(DebounceDelay, Timeout.InfiniteTimeSpan);
        _debounceTimer ??= new Timer(OnDebounceElapsed, null, DebounceDelay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Cancels the debounce timer. Must be called under <see cref="_lock"/>.</summary>
    private void CancelTimerLocked()
    {
        _debounceTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Timer callback — fires on a thread-pool thread after the quiet period.</summary>
    private void OnDebounceElapsed(object? state)
    {
        Guid sessionId;
        string? data;

        lock (_lock)
        {
            sessionId = _currentSessionId;
            data = _pendingData;
            _pendingData = null;
        }

        if (data is not null && sessionId != Guid.Empty)
            _ = SaveNowAsync(sessionId, data);
    }

    // ── Persistence helpers ────────────────────────────────────────

    private async Task SaveNowAsync(Guid sessionId, string serializedBillData)
    {
        lock (_lock)
        {
            _isSaving = true;

            // Clear pending if it matches what we're about to write
            if (_pendingData == serializedBillData)
                _pendingData = null;
        }

        try
        {
            await _persistence.UpdateCartAsync(sessionId, serializedBillData)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Auto-save failed for session {SessionId}", sessionId);
        }
        finally
        {
            lock (_lock) _isSaving = false;
        }
    }

    private async Task MarkSessionInactiveAsync(Guid sessionId, bool completed)
    {
        try
        {
            if (completed)
                await _persistence.MarkCompletedAsync(sessionId).ConfigureAwait(false);
            else
                await _persistence.MarkCancelledAsync(sessionId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to mark session {SessionId} as {Status}",
                sessionId, completed ? "completed" : "cancelled");
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Unsubscribe<CartChangedEvent>(OnCartChangedAsync);
        _eventBus.Unsubscribe<PaymentStartedEvent>(OnPaymentStartedAsync);
        _eventBus.Unsubscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Unsubscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);

        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }
}
