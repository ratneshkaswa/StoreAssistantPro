namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable snapshot of operator interaction metrics over a sliding
/// time window. Produced by <see cref="Core.Services.IInteractionTracker"/>
/// and consumed by <see cref="Core.Services.IFlowStateEngine"/> to
/// refine flow-state transitions.
///
/// <para>All frequency values are computed over the tracker's sliding
/// window (default 5 seconds) — not lifetime averages.</para>
/// </summary>
/// <param name="KeyboardFrequency">
/// Key-press events per second in the sliding window.
/// Zero when the operator has not typed recently.
/// </param>
/// <param name="MouseFrequency">
/// Mouse-move events per second in the sliding window.
/// Coalesced — rapid sub-pixel moves count as one event.
/// </param>
/// <param name="IdleSeconds">
/// Seconds since the most recent keyboard or mouse event.
/// Resets to zero on any input. Capped at
/// <see cref="Core.Services.InteractionTracker.MaxIdleSeconds"/>.
/// </param>
/// <param name="BillingActionsPerMinute">
/// Billing-domain actions (cart adds, quantity changes, payment steps)
/// per minute in the sliding window. Indicates the operator's
/// transaction throughput.
/// </param>
/// <param name="CapturedAt">
/// Timestamp (IST) when this snapshot was computed.
/// </param>
public sealed record InteractionSnapshot(
    double KeyboardFrequency,
    double MouseFrequency,
    double IdleSeconds,
    double BillingActionsPerMinute,
    DateTime CapturedAt)
{
    /// <summary>A zero-activity snapshot.</summary>
    public static InteractionSnapshot Idle(DateTime now) =>
        new(0, 0, 0, 0, now);

    /// <summary>
    /// <c>true</c> when the operator shows sustained rapid input —
    /// keyboard frequency above the flow threshold and idle time near zero.
    /// </summary>
    public bool IsRapidInput => KeyboardFrequency >= 2.0 && IdleSeconds < 1.5;

    /// <summary>
    /// <c>true</c> when no input has been detected for a meaningful
    /// period (operator paused or stepped away).
    /// </summary>
    public bool IsIdle => IdleSeconds >= 3.0;
}
