using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// The visual feedback type for a zero-click micro-feedback pulse.
/// </summary>
public enum MicroFeedbackType
{
    /// <summary>Soft green success pulse — product added, PIN accepted.</summary>
    Success,

    /// <summary>Brief accent highlight — item selected, action confirmed.</summary>
    Confirm
}

/// <summary>
/// Published when a zero-click action completes and the UI should show
/// a brief micro-feedback pulse. Subscribers (attached behaviors) play
/// an under-120ms animation on the relevant element.
/// </summary>
/// <param name="TargetId">
/// Identifies which UI element should pulse.
/// <list type="bullet">
///   <item><c>"Cart:LastRow"</c> — the most recently added cart row.</item>
///   <item><c>"PinPad"</c> — the PIN display dots.</item>
///   <item><c>"Search"</c> — the billing search box.</item>
/// </list>
/// </param>
/// <param name="Type">The visual style of the feedback pulse.</param>
/// <param name="Label">Human-readable description for diagnostics.</param>
public sealed record MicroFeedbackEvent(
    string TargetId,
    MicroFeedbackType Type,
    string Label) : IEvent;
