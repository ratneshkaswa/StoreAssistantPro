using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Drives keyboard focus through the billing workflow by translating
/// billing lifecycle events into <see cref="IPredictiveFocusService"/>
/// focus hints.
/// <para>
/// <b>Focus flow:</b>
/// </para>
/// <code>
///   ┌──────────────────────────┬───────────────────────────────────┐
///   │ Billing Event            │ Focus Target                      │
///   ├──────────────────────────┼───────────────────────────────────┤
///   │ Session started          │ BillingSearchBox (product input)  │
///   │ Item added (cart change) │ BillingSearchBox (scan next item) │
///   │ Payment started          │ PaymentAmountInput                │
///   │ Session completed        │ BillingSearchBox (next customer)  │
///   │ Session cancelled        │ BillingSearchBox (fresh start)    │
///   └──────────────────────────┴───────────────────────────────────┘
/// </code>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b> in <see cref="BillingModule"/>.</item>
///   <item>Lives in the Billing module — depends on billing events.</item>
///   <item>Delegates all focus decisions to <see cref="IPredictiveFocusService"/>
///         which handles user-input suppression and hint emission.</item>
///   <item>The <see cref="IFocusLockService"/> is checked to ensure focus
///         hints are only emitted while billing is active.</item>
///   <item>Implements <see cref="IDisposable"/> for deterministic cleanup
///         of event subscriptions.</item>
/// </list>
///
/// <para><b>Zero-click guarantee:</b></para>
/// <para>
/// Every billing state transition automatically places keyboard focus
/// on the correct input field. The operator can scan barcodes, type
/// product names, enter quantities, and process payments without ever
/// reaching for the mouse.
/// </para>
/// </summary>
public sealed class BillingFocusCoordinator : IDisposable
{
    private readonly IPredictiveFocusService _focusService;
    private readonly IFocusLockService _focusLock;
    private readonly IEventBus _eventBus;

    /// <summary>
    /// The product search/barcode input — primary landing target during billing.
    /// Matches the <c>x:Name</c> in the billing view and the constant in
    /// <see cref="PredictiveFocusService"/>.
    /// </summary>
    internal const string BillingSearchBox = "BillingSearchBox";

    /// <summary>
    /// The payment amount input — landing target during payment processing.
    /// </summary>
    internal const string PaymentAmountInput = "PaymentAmountInput";

    public BillingFocusCoordinator(
        IPredictiveFocusService focusService,
        IFocusLockService focusLock,
        IEventBus eventBus)
    {
        _focusService = focusService;
        _focusLock = focusLock;
        _eventBus = eventBus;

        _eventBus.Subscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Subscribe<CartChangedEvent>(OnCartChangedAsync);
        _eventBus.Subscribe<PaymentStartedEvent>(OnPaymentStartedAsync);
        _eventBus.Subscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Subscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);
    }

    // ── Session started: land on product search ──────────────────────

    private Task OnSessionStartedAsync(BillingSessionStartedEvent _)
    {
        if (_focusLock.IsFocusLocked)
            _focusService.RequestFocus(BillingSearchBox, "BillingSessionStarted");

        return Task.CompletedTask;
    }

    // ── Item added: return to product search for next scan ──────────

    private Task OnCartChangedAsync(CartChangedEvent _)
    {
        // Only redirect focus if billing is locked — prevents stray
        // focus moves if the cart is being restored in the background.
        if (_focusLock.IsFocusLocked)
            _focusService.RequestFocus(BillingSearchBox, "CartChanged");

        return Task.CompletedTask;
    }

    // ── Payment started: land on payment input ──────────────────────

    private Task OnPaymentStartedAsync(PaymentStartedEvent _)
    {
        if (_focusLock.IsFocusLocked)
            _focusService.RequestFocus(PaymentAmountInput, "PaymentStarted");

        return Task.CompletedTask;
    }

    // ── Session completed: reset for next customer ──────────────────

    private Task OnSessionCompletedAsync(BillingSessionCompletedEvent _)
    {
        // After payment completes and the bill is saved, the focus lock
        // may still be held briefly (SmartBillingModeService processes
        // the stop asynchronously). Request focus immediately — the
        // PredictiveFocusBehavior will execute the hint when the UI is
        // ready, even if the lock releases a frame later.
        _focusService.RequestFocus(BillingSearchBox, "BillingSessionCompleted");

        return Task.CompletedTask;
    }

    // ── Session cancelled: reset for fresh start ────────────────────

    private Task OnSessionCancelledAsync(BillingSessionCancelledEvent _)
    {
        _focusService.RequestFocus(BillingSearchBox, "BillingSessionCancelled");

        return Task.CompletedTask;
    }

    // ── Cleanup ─────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<BillingSessionStartedEvent>(OnSessionStartedAsync);
        _eventBus.Unsubscribe<CartChangedEvent>(OnCartChangedAsync);
        _eventBus.Unsubscribe<PaymentStartedEvent>(OnPaymentStartedAsync);
        _eventBus.Unsubscribe<BillingSessionCompletedEvent>(OnSessionCompletedAsync);
        _eventBus.Unsubscribe<BillingSessionCancelledEvent>(OnSessionCancelledAsync);
    }
}
