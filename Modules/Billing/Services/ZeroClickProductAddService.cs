using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Bridges the intent detection system to the billing cart.
/// <para>
/// Subscribes to <see cref="IntentDetectedEvent"/> and, when a barcode
/// scan or exact product match is detected with high confidence during
/// an active billing session, automatically adds the product to the
/// cart, clears the input, and returns focus to the search box.
/// </para>
///
/// <para><b>Flow:</b></para>
/// <code>
///   IntentDetectedEvent
///     │
///     ├─ BarcodeScan?  ──► FindByBarcodeAsync  ──► ConfidenceEvaluator
///     ├─ ExactMatch?   ──► FindByExactTextAsync ──► ConfidenceEvaluator
///     │
///     └─ Verdict.ShouldAutoExecute?
///           ├─ Yes ──► Publish ProductAddedToCartEvent
///           │          Request focus → BillingSearchBox
///           └─ No  ──► Log rejection, do nothing
/// </code>
///
/// <para><b>Safety gates:</b></para>
/// <list type="bullet">
///   <item>Billing session must be <see cref="BillingSessionState.Active"/>.</item>
///   <item>Application must not be offline.</item>
///   <item>Product must be active and in stock.</item>
///   <item><see cref="ConfidenceEvaluator"/> must return
///         <see cref="ConfidenceVerdict.ShouldAutoExecute"/>.</item>
/// </list>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as <b>singleton</b> in <see cref="BillingModule"/>.</item>
///   <item>No UI dependency — communicates via events and focus hints.</item>
///   <item>Works with <see cref="IPredictiveFocusService"/> for focus return.</item>
///   <item>Works with <see cref="BillingFocusCoordinator"/> which handles
///         the <see cref="CartChangedEvent"/> → focus return independently.</item>
/// </list>
/// </summary>
public sealed class ZeroClickProductAddService : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IProductService _productService;
    private readonly IBillingSessionService _billingSession;
    private readonly IAppStateService _appState;
    private readonly IPredictiveFocusService _focusService;
    private readonly IPerformanceMonitor _perf;
    private readonly ILogger<ZeroClickProductAddService> _logger;
    private bool _disposed;

    public ZeroClickProductAddService(
        IEventBus eventBus,
        IProductService productService,
        IBillingSessionService billingSession,
        IAppStateService appState,
        IPredictiveFocusService focusService,
        IPerformanceMonitor perf,
        ILogger<ZeroClickProductAddService> logger)
    {
        _eventBus = eventBus;
        _productService = productService;
        _billingSession = billingSession;
        _appState = appState;
        _focusService = focusService;
        _perf = perf;
        _logger = logger;

        _eventBus.Subscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
    }

    // ── Event handler ────────────────────────────────────────────────

    internal async Task OnIntentDetectedAsync(IntentDetectedEvent evt)
    {
        var intent = evt.Result;

        // Only handle barcode scans and exact product matches
        if (intent.Intent is not (InputIntent.BarcodeScan or InputIntent.ExactProductMatch))
            return;

        // Only act in billing search context
        if (intent.Context is not (InputContext.BillingSearch or InputContext.ProductSearch))
            return;

        // Safety: billing session must be active
        if (_billingSession.CurrentState != BillingSessionState.Active)
        {
            _logger.LogDebug(
                "ZeroClickProductAdd skipped: billing session is {State}.",
                _billingSession.CurrentState);
            return;
        }

        // Safety: not offline
        if (_appState.IsOfflineMode)
        {
            _logger.LogDebug("ZeroClickProductAdd skipped: app is offline.");
            return;
        }

        using var scope = _perf.BeginScope("ZeroClickProductAdd.Process");

        try
        {
            await ProcessIntentAsync(intent).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ZeroClickProductAdd failed for input '{Input}'.", intent.RawInput);
        }
    }

    // ── Core logic ───────────────────────────────────────────────────

    private async Task ProcessIntentAsync(IntentResult intent)
    {
        // Look up product(s) based on intent type
        var (products, source) = intent.Intent switch
        {
            InputIntent.BarcodeScan => (
                await _productService.FindByBarcodeAsync(intent.RawInput).ConfigureAwait(false),
                "Barcode"),
            InputIntent.ExactProductMatch => (
                await _productService.FindByExactTextAsync(intent.RawInput).ConfigureAwait(false),
                "ExactMatch"),
            _ => ((IReadOnlyList<Product>)[], "Unknown")
        };

        // Evaluate confidence
        var verdict = intent.Intent switch
        {
            InputIntent.BarcodeScan =>
                ConfidenceEvaluator.EvaluateBarcodeScan(intent, products.Count),
            InputIntent.ExactProductMatch =>
                ConfidenceEvaluator.EvaluateProductMatch(intent, products.Count),
            _ => ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                    "Unsupported intent type.")
        };

        if (!verdict.ShouldAutoExecute)
        {
            _logger.LogDebug(
                "ZeroClickProductAdd rejected: {Reason} — {Explanation}",
                verdict.Rejection, verdict.Explanation);
            return;
        }

        var product = products[0];

        // Safety: product must be in stock
        if (product.Quantity <= 0)
        {
            _logger.LogDebug(
                "ZeroClickProductAdd skipped: product '{Name}' is out of stock.",
                product.Name);
            return;
        }

        // Auto-add to cart
        _logger.LogInformation(
            "ZeroClickProductAdd: auto-adding '{Name}' (Id={Id}) via {Source}.",
            product.Name, product.Id, source);

        await _eventBus.PublishAsync(new ProductAddedToCartEvent(
            product.Id,
            product.Name,
            Quantity: 1,
            product.SalePrice,
            source)).ConfigureAwait(false);

        // Return focus to billing search box
        _focusService.RequestFocus(
            BillingFocusCoordinator.BillingSearchBox,
            $"ZeroClickProductAdd:{source}");
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _eventBus.Unsubscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
    }
}
