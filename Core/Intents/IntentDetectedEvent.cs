using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Published when the <see cref="IIntentDetectionService"/> classifies
/// input with confidence above threshold.
/// <para>
/// Subscribers (billing module, search auto-complete, PIN pad) react
/// to this event to trigger context-appropriate actions without tight
/// coupling to the input source.
/// </para>
/// </summary>
public sealed record IntentDetectedEvent(IntentResult Result) : IEvent;
