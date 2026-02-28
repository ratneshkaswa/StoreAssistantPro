using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Singleton implementation of <see cref="IZeroClickPinService"/>.
/// <para>
/// Subscribes to <see cref="IntentDetectedEvent"/>. When a
/// <see cref="InputIntent.PinCompleted"/> intent arrives:
/// <list type="number">
///   <item>Evaluates via <see cref="ConfidenceEvaluator.EvaluatePinCompleted"/>.</item>
///   <item>If auto-execute: calls the registered handler.</item>
///   <item>On success: publishes <see cref="PinAutoSubmittedEvent"/>.</item>
///   <item>On failure: publishes <see cref="PinSubmissionFailedEvent"/>
///         and requests focus return to the PIN input.</item>
/// </list>
/// </para>
/// </summary>
public sealed class ZeroClickPinService : IZeroClickPinService
{
    private readonly IEventBus _eventBus;
    private readonly IPredictiveFocusService _focusService;
    private readonly IPerformanceMonitor _perf;
    private readonly ILogger<ZeroClickPinService> _logger;

    private IZeroClickPinService.PinSubmitHandler? _handler;
    private string _focusElementName = string.Empty;
    private bool _disposed;

    public ZeroClickPinService(
        IEventBus eventBus,
        IPredictiveFocusService focusService,
        IPerformanceMonitor perf,
        ILogger<ZeroClickPinService> logger)
    {
        _eventBus = eventBus;
        _focusService = focusService;
        _perf = perf;
        _logger = logger;

        _eventBus.Subscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
    }

    /// <inheritdoc/>
    public bool IsHandlerRegistered => _handler is not null;

    /// <inheritdoc/>
    public bool IsSubmitting { get; private set; }

    /// <inheritdoc/>
    public void RegisterHandler(
        IZeroClickPinService.PinSubmitHandler handler,
        string focusElementName)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _focusElementName = focusElementName;
        _logger.LogDebug(
            "ZeroClickPin handler registered, focus element: '{Element}'.",
            focusElementName);
    }

    /// <inheritdoc/>
    public void UnregisterHandler()
    {
        _handler = null;
        _focusElementName = string.Empty;
        _logger.LogDebug("ZeroClickPin handler unregistered.");
    }

    // ── Event handler ────────────────────────────────────────────────

    private async Task OnIntentDetectedAsync(IntentDetectedEvent evt)
    {
        var intent = evt.Result;

        // Only handle PIN completions
        if (intent.Intent != InputIntent.PinCompleted)
            return;

        // Must have a registered handler
        if (_handler is null)
        {
            _logger.LogDebug("ZeroClickPin: no handler registered, skipping.");
            return;
        }

        // Prevent double-submission
        if (IsSubmitting)
        {
            _logger.LogDebug("ZeroClickPin: already submitting, skipping.");
            return;
        }

        // Evaluate confidence
        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);
        if (!verdict.ShouldAutoExecute)
        {
            _logger.LogDebug(
                "ZeroClickPin rejected: {Reason} — {Explanation}",
                verdict.Rejection, verdict.Explanation);
            return;
        }

        var pinType = intent.ResolvedValue
            ?? (intent.RawInput.Length == 4 ? "UserPin" : "MasterPin");

        using var scope = _perf.BeginScope("ZeroClickPin.Submit");

        IsSubmitting = true;
        try
        {
            _logger.LogDebug(
                "ZeroClickPin: auto-submitting {PinType} ({Length} digits).",
                pinType, intent.RawInput.Length);

            var result = await _handler(intent.RawInput, pinType)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "ZeroClickPin: {PinType} accepted.", pinType);

                await _eventBus
                    .PublishAsync(new PinAutoSubmittedEvent(pinType))
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug(
                    "ZeroClickPin: {PinType} rejected — {Error}.",
                    pinType, result.ErrorMessage);

                await _eventBus
                    .PublishAsync(new PinSubmissionFailedEvent(pinType, result.ErrorMessage))
                    .ConfigureAwait(false);

                // Return focus to PIN input for retry
                if (!string.IsNullOrEmpty(_focusElementName))
                {
                    _focusService.RequestFocus(
                        _focusElementName,
                        $"ZeroClickPin:{pinType}:Failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ZeroClickPin: handler threw for {PinType}.", pinType);

            await _eventBus
                .PublishAsync(new PinSubmissionFailedEvent(pinType, "An unexpected error occurred."))
                .ConfigureAwait(false);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _eventBus.Unsubscribe<IntentDetectedEvent>(OnIntentDetectedAsync);
        _handler = null;
    }
}
