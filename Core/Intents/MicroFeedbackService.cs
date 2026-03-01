using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Singleton implementation of <see cref="IMicroFeedbackService"/>.
/// Subscribes to zero-click domain events and publishes
/// <see cref="MicroFeedbackEvent"/> for UI-side micro-animations.
/// </summary>
public sealed class MicroFeedbackService : IMicroFeedbackService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<MicroFeedbackService> _logger;
    private bool _disposed;

    public MicroFeedbackService(
        IEventBus eventBus,
        ILogger<MicroFeedbackService> logger)
    {
        _eventBus = eventBus;
        _logger = logger;

        _eventBus.Subscribe<PinAutoSubmittedEvent>(OnPinAutoSubmittedAsync);
        _eventBus.Subscribe<ZeroClickActionExecutedEvent>(OnZeroClickActionAsync);
    }

    // ── Event handlers ───────────────────────────────────────────────

    private async Task OnPinAutoSubmittedAsync(PinAutoSubmittedEvent evt)
    {
        _logger.LogDebug(
            "MicroFeedback: {PinType} accepted — pulsing PIN pad.",
            evt.PinType);

        await _eventBus.PublishAsync(new MicroFeedbackEvent(
            TargetId: "PinPad",
            Type: MicroFeedbackType.Success,
            Label: $"{evt.PinType} accepted"));
    }

    private async Task OnZeroClickActionAsync(ZeroClickActionExecutedEvent evt)
    {
        _logger.LogDebug(
            "MicroFeedback: zero-click '{Rule}' — confirm pulse.",
            evt.RuleId);

        await _eventBus.PublishAsync(new MicroFeedbackEvent(
            TargetId: evt.RuleId,
            Type: MicroFeedbackType.Confirm,
            Label: evt.Description));
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _eventBus.Unsubscribe<PinAutoSubmittedEvent>(OnPinAutoSubmittedAsync);
        _eventBus.Unsubscribe<ZeroClickActionExecutedEvent>(OnZeroClickActionAsync);
    }
}
