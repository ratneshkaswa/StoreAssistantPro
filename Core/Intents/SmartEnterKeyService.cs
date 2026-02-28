using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Singleton implementation of <see cref="ISmartEnterKeyService"/>.
/// <para>
/// Subscribes to <see cref="IntentDetectedEvent"/> to maintain a cache
/// of the latest intent classification. When <see cref="Evaluate"/> is
/// called synchronously by the keyboard handler, it checks the cached
/// intent against <see cref="ConfidenceEvaluator"/> to decide.
/// </para>
///
/// <para><b>Thread safety:</b></para>
/// <para>
/// <see cref="UpdateLatestIntent"/> may be called from background threads
/// (event bus), while <see cref="Evaluate"/> is called from the UI thread.
/// The <c>volatile</c> field ensures visibility without locking.
/// </para>
/// </summary>
public sealed class SmartEnterKeyService : ISmartEnterKeyService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<SmartEnterKeyService> _logger;
    private volatile IntentResult? _latestIntent;
    private volatile bool _executing;
    private bool _disposed;

    public SmartEnterKeyService(
        IEventBus eventBus,
        ILogger<SmartEnterKeyService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;

        _eventBus.Subscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
    }

    /// <inheritdoc/>
    public IntentResult? LatestIntent => _latestIntent;

    /// <inheritdoc/>
    public void UpdateLatestIntent(IntentResult intent) =>
        _latestIntent = intent;

    /// <inheritdoc/>
    public void ClearLatestIntent() =>
        _latestIntent = null;

    /// <inheritdoc/>
    public EnterKeyDecision Evaluate(string inputText, InputContext context)
    {
        // ── Guard: previous action still running ─────────────────
        if (_executing)
            return EnterKeyDecision.Suppress();

        // ── Guard: empty input always moves next ─────────────────
        if (string.IsNullOrWhiteSpace(inputText))
            return EnterKeyDecision.MoveNext("Empty input");

        // ── Guard: form/general context always moves next ────────
        if (context is InputContext.General or InputContext.PinEntry)
            return EnterKeyDecision.MoveNext($"Context {context} — standard navigation");

        // ── Check cached intent ──────────────────────────────────
        var intent = _latestIntent;

        // No cached intent or stale (input text changed since last classification)
        if (intent is null || intent.RawInput != inputText)
            return EnterKeyDecision.MoveNext("No matching cached intent");

        // Unknown intent → standard navigation
        if (intent.Intent == InputIntent.Unknown)
            return EnterKeyDecision.MoveNext($"Unknown intent for '{inputText}'");

        // ── Evaluate confidence using cached match count ─────────
        var verdict = ConfidenceEvaluator.Evaluate(intent);

        if (verdict.ShouldAutoExecute)
        {
            var actionId = intent.Intent switch
            {
                InputIntent.BarcodeScan => "AutoAddProduct:Barcode",
                InputIntent.ExactProductMatch => "AutoAddProduct:ExactMatch",
                InputIntent.PinCompleted => "AutoSubmitPin",
                InputIntent.AutoCompleteTrigger => "AutoSelectSingle",
                _ => $"AutoAction:{intent.Intent}"
            };

            _logger.LogDebug(
                "SmartEnter: Execute {ActionId} for '{Input}'.",
                actionId, inputText);

            return EnterKeyDecision.Execute(actionId, verdict.Explanation);
        }

        _logger.LogDebug(
            "SmartEnter: {Confidence} ({Reason}) — MoveNext for '{Input}'.",
            verdict.Confidence, verdict.Rejection, inputText);

        return EnterKeyDecision.MoveNext(verdict.Explanation);
    }

    // ── Intent event listener ────────────────────────────────────────

    private Task OnIntentDetectedAsync(IntentDetectedEvent evt)
    {
        _latestIntent = evt.Result;
        return Task.CompletedTask;
    }

    // ── Executing flag (set by KeyboardNav) ──────────────────────────

    /// <summary>
    /// Marks the service as executing an auto-action. While set,
    /// <see cref="Evaluate"/> returns <see cref="EnterKeyAction.Suppress"/>.
    /// Must be called from the UI thread.
    /// </summary>
    internal void MarkExecuting() => _executing = true;

    /// <summary>
    /// Clears the executing flag. Must be called from the UI thread
    /// after the auto-action completes (or fails).
    /// </summary>
    internal void ClearExecuting() => _executing = false;

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _eventBus.Unsubscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
        _latestIntent = null;
    }
}
