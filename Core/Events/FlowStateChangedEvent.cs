using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IFlowStateEngine"/> whenever the
/// operator's <see cref="FlowState"/> changes. UI systems subscribe
/// to adapt animation intensity, notification priority, and chrome
/// visibility without polling.
/// </summary>
/// <param name="Previous">The state before the transition.</param>
/// <param name="Current">The new active state.</param>
/// <param name="TransitionReason">
/// Human-readable explanation of why the transition occurred
/// (for diagnostics and logging).
/// </param>
public sealed record FlowStateChangedEvent(
    FlowState Previous,
    FlowState Current,
    string TransitionReason) : IEvent;
