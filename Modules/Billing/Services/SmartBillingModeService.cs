using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Reacts to granular billing session events and drives
/// <see cref="IBillingModeService"/> to keep the operational mode in
/// sync with the session lifecycle.
/// <para>
/// <b>Safety rules enforced:</b>
/// </para>
/// <list type="number">
///   <item><b>Active-session guard:</b> Will not exit Billing mode while
///         <see cref="IBillingSessionService.CurrentState"/> is
///         <see cref="BillingSessionState.Active"/>.</item>
///   <item><b>Payment lock:</b> All mode transitions are blocked while
///         <see cref="IsPaymentProcessing"/> is <c>true</c>. A pending
///         stop is deferred and flushed when
///         <see cref="EndPaymentProcessingAsync"/> is called.</item>
///   <item><b>Focus lock hold:</b> <see cref="IFocusLockService.HoldRelease"/>
///         is called when payment starts, preventing the UI focus lock
///         from releasing mid-payment. <see cref="IFocusLockService.LiftReleaseHold"/>
///         is called when payment ends, flushing any deferred release.</item>
///   <item><b>Serialized transitions:</b> A <see cref="SemaphoreSlim"/>
///         ensures only one transition decision runs at a time, preventing
///         race conditions from concurrent events.</item>
/// </list>
/// </summary>
public class SmartBillingModeService : ISmartBillingModeService
{
    private readonly IBillingModeService _modeService;
    private readonly IBillingSessionService _sessionService;
    private readonly IFocusLockService _focusLock;
    private readonly IEventBus _eventBus;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _paymentLock = new();

    private bool _isPaymentProcessing;
    private bool _stopPendingAfterPayment;

    public SmartBillingModeService(
        IBillingModeService modeService,
        IBillingSessionService sessionService,
        IFocusLockService focusLock,
        IEventBus eventBus)
    {
        _modeService = modeService;
        _sessionService = sessionService;
        _focusLock = focusLock;
        _eventBus = eventBus;

        _eventBus.Subscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Subscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Subscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);
    }

    // ── Payment processing lock ────────────────────────────────────

    public bool IsPaymentProcessing
    {
        get { lock (_paymentLock) return _isPaymentProcessing; }
    }

    public void BeginPaymentProcessing()
    {
        lock (_paymentLock)
        {
            if (_isPaymentProcessing)
                throw new InvalidOperationException(
                    "Payment processing is already in progress.");

            _isPaymentProcessing = true;
            _stopPendingAfterPayment = false;
        }

        _focusLock.HoldRelease();
    }

    public async Task EndPaymentProcessingAsync()
    {
        bool shouldFlush;

        lock (_paymentLock)
        {
            if (!_isPaymentProcessing)
                throw new InvalidOperationException(
                    "No payment is currently processing.");

            _isPaymentProcessing = false;
            shouldFlush = _stopPendingAfterPayment;
            _stopPendingAfterPayment = false;
        }

        _focusLock.LiftReleaseHold();

        if (shouldFlush)
            await ExecuteStopAsync();
    }

    // ── Event handlers ─────────────────────────────────────────────

    private async Task OnSessionStartedAsync(BillingSessionStartedEvent _)
    {
        await _gate.WaitAsync();
        try
        {
            // Starting billing is always safe — no guard needed.
            await _modeService.StartBillingAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task OnSessionCompletedAsync(BillingSessionCompletedEvent _)
    {
        await _gate.WaitAsync();
        try
        {
            await SafeStopBillingAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task OnSessionCancelledAsync(BillingSessionCancelledEvent _)
    {
        await _gate.WaitAsync();
        try
        {
            await SafeStopBillingAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    // ── Core safety logic ──────────────────────────────────────────

    /// <summary>
    /// Attempts to stop billing with all safety checks applied.
    /// Must be called while holding <see cref="_gate"/>.
    /// </summary>
    private async Task SafeStopBillingAsync()
    {
        // Rule 1: Never exit billing while a session is active.
        if (_sessionService.CurrentState == BillingSessionState.Active)
            return;

        // Rule 2: Defer if payment is processing.
        lock (_paymentLock)
        {
            if (_isPaymentProcessing)
            {
                _stopPendingAfterPayment = true;
                return;
            }
        }

        await ExecuteStopAsync();
    }

    /// <summary>
    /// Executes the actual mode switch + deferred flush.
    /// </summary>
    private async Task ExecuteStopAsync()
    {
        await _modeService.StopBillingAsync();
        await _modeService.FlushDeferredStopAsync();
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Unsubscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Unsubscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);
        _gate.Dispose();
    }
}
