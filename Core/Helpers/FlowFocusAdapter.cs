using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Pure, static focus-adaptation logic. Maps a <see cref="FlowState"/>
/// to focus behavior parameters that <see cref="Core.Services.PredictiveFocusService"/>
/// uses to adjust its response speed and aggressiveness.
///
/// <para><b>No DI, no UI, no side effects.</b> Every method is a
/// deterministic pure function.</para>
///
/// <para><b>Adaptation matrix:</b></para>
/// <code>
///   ┌──────────┬──────────────┬────────────────┬───────────────────────────┐
///   │ State    │ IdleTimeout  │ PriorityBoost  │ BypassInputGuard          │
///   ├──────────┼──────────────┼────────────────┼───────────────────────────┤
///   │ Calm     │ 600 ms       │    0           │ false (standard)          │
///   │ Focused  │ 400 ms       │    5           │ false (standard)          │
///   │ Flow     │ 200 ms       │   10           │ true  (aggressive)        │
///   └──────────┴──────────────┴────────────────┴───────────────────────────┘
/// </code>
///
/// <para><b>Idle timeout:</b> Controls how quickly the user-input
/// suppression lifts after the last keystroke. Shorter timeout in Flow
/// means the service responds faster after the operator pauses.</para>
///
/// <para><b>Priority boost:</b> Added to the base priority of emitted
/// <see cref="FocusHint"/>s. Flow state gets the highest boost,
/// ensuring predictive hints win over lower-priority manual focus
/// requests in the dispatcher queue.</para>
///
/// <para><b>Bypass input guard:</b> In Flow state, the user is in
/// rapid-fire scanning/entry mode. Programmatic focus hints should
/// track them aggressively instead of being suppressed by the
/// <c>IsUserInputActive</c> guard. This ensures the search box
/// stays targeted between rapid barcode scans.</para>
/// </summary>
public static class FlowFocusAdapter
{
    // ── Idle timeout per state ────────────────────────────────────────

    /// <summary>Calm: standard idle timeout (ms).</summary>
    public const int CalmIdleTimeoutMs = 600;

    /// <summary>Focused: faster idle timeout (ms).</summary>
    public const int FocusedIdleTimeoutMs = 400;

    /// <summary>Flow: minimal idle timeout (ms).</summary>
    public const int FlowIdleTimeoutMs = 200;

    // ── Priority boost per state ─────────────────────────────────────

    /// <summary>Calm: no priority boost.</summary>
    public const int CalmPriorityBoost = 0;

    /// <summary>Focused: moderate priority boost.</summary>
    public const int FocusedPriorityBoost = 5;

    /// <summary>Flow: maximum priority boost.</summary>
    public const int FlowPriorityBoost = 10;

    // ── Core API ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the idle timeout in milliseconds for the given flow state.
    /// Shorter timeout means the input-suppression guard lifts faster.
    /// </summary>
    public static int GetIdleTimeoutMs(FlowState state) => state switch
    {
        FlowState.Flow => FlowIdleTimeoutMs,
        FlowState.Focused => FocusedIdleTimeoutMs,
        _ => CalmIdleTimeoutMs
    };

    /// <summary>
    /// Returns the priority boost to add to emitted focus hints.
    /// </summary>
    public static int GetPriorityBoost(FlowState state) => state switch
    {
        FlowState.Flow => FlowPriorityBoost,
        FlowState.Focused => FocusedPriorityBoost,
        _ => CalmPriorityBoost
    };

    /// <summary>
    /// Returns <c>true</c> when the input guard should be bypassed,
    /// allowing aggressive auto-focus during peak concentration.
    /// </summary>
    public static bool ShouldBypassInputGuard(FlowState state) =>
        state == FlowState.Flow;
}
