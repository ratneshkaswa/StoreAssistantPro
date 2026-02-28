namespace StoreAssistantPro.Models;

/// <summary>
/// Represents the operator's current cognitive engagement level.
/// Used by the <see cref="Core.Services.IFlowStateEngine"/> to drive
/// UI density, animation intensity, and notification priority.
///
/// <para><b>State definitions:</b></para>
/// <code>
///   ┌─────────┬──────────────────────────────────────────────────────┐
///   │ State   │ When                                                 │
///   ├─────────┼──────────────────────────────────────────────────────┤
///   │ Calm    │ Management mode, or Billing with no active session.  │
///   │         │ Browse, configure, review — low urgency.             │
///   ├─────────┼──────────────────────────────────────────────────────┤
///   │ Focused │ Billing session active, operator building cart or    │
///   │         │ reviewing items — moderate concentration.            │
///   ├─────────┼──────────────────────────────────────────────────────┤
///   │ Flow    │ Active input detected during billing — rapid scan,  │
///   │         │ barcode entry, PIN input. Peak concentration, zero   │
///   │         │ distractions.                                        │
///   └─────────┴──────────────────────────────────────────────────────┘
/// </code>
///
/// <para><b>State machine:</b></para>
/// <code>
///   Calm ──▶ Focused  (billing session starts)
///   Focused ──▶ Flow  (active typing/input detected)
///   Flow ──▶ Focused  (input idle timeout)
///   Focused ──▶ Calm  (session completes/cancels, or mode → Management)
///   Flow ──▶ Calm     (session completes/cancels, or mode → Management)
/// </code>
/// </summary>
public enum FlowState
{
    /// <summary>
    /// Low urgency — Management mode or idle billing. Full chrome,
    /// all notifications, normal animation intensity.
    /// </summary>
    Calm,

    /// <summary>
    /// Moderate concentration — active billing session, building cart.
    /// Chrome muted, non-critical notifications deferred.
    /// </summary>
    Focused,

    /// <summary>
    /// Peak concentration — rapid input/scanning detected during billing.
    /// Chrome fully receded, notifications suppressed, zero-click
    /// active, minimal animation.
    /// </summary>
    Flow
}
