using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Pure, static motion-adaptation logic. Maps a <see cref="FlowState"/>
/// to a duration multiplier that scales animation timings for the
/// operator's current cognitive engagement level.
///
/// <para>
/// <b>No DI, no UI, no side effects.</b> Every method is a deterministic
/// pure function.
/// </para>
///
/// <para><b>Duration multipliers:</b></para>
/// <code>
///   ┌──────────┬────────────┬──────────────────────────────────────┐
///   │ State    │ Multiplier │ Effect                               │
///   ├──────────┼────────────┼──────────────────────────────────────┤
///   │ Calm     │    1.0     │ Normal animations — full chrome.     │
///   │ Focused  │    0.65    │ Slightly faster — moderate focus.    │
///   │ Flow     │    0.2     │ Minimal duration — peak focus.       │
///   └──────────┴────────────┴──────────────────────────────────────┘
///
///   base duration × multiplier = adapted duration
///
///   Example: FluentDurationSlow (200 ms)
///     Calm    → 200 ms (normal)
///     Focused → 130 ms (brisk)
///     Flow    →  40 ms (near-instant, no visual jump)
/// </code>
///
/// <para><b>Minimum duration floor:</b></para>
/// <para>
/// Adapted durations are clamped to a minimum of 16 ms (one frame at
/// 60 fps) to prevent zero-duration jumps. This ensures all transitions
/// remain smooth even at the Flow multiplier.
/// </para>
///
/// <para><b>Integration:</b> <see cref="Motion.GetDuration"/> calls
/// <see cref="GetAdaptedDuration"/> when a <see cref="FlowState"/> is
/// available from the element's attached property chain.</para>
/// </summary>
public static class FlowMotionAdapter
{
    // ── Multipliers ──────────────────────────────────────────────────

    /// <summary>Calm state: normal animation speed.</summary>
    public const double CalmMultiplier = 1.0;

    /// <summary>Focused state: slightly faster animations.</summary>
    public const double FocusedMultiplier = 0.65;

    /// <summary>Flow state: minimal animation duration.</summary>
    public const double FlowMultiplier = 0.2;

    /// <summary>
    /// Minimum adapted duration in milliseconds. Prevents zero-duration
    /// jumps — one frame at 60 fps ensures smooth visual continuity.
    /// </summary>
    public const double MinDurationMs = 16.0;

    // ── Core API ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the duration multiplier for the given flow state.
    /// </summary>
    public static double GetDurationMultiplier(FlowState state) => state switch
    {
        FlowState.Flow => FlowMultiplier,
        FlowState.Focused => FocusedMultiplier,
        _ => CalmMultiplier
    };

    /// <summary>
    /// Adapts a base duration by applying the flow-state multiplier
    /// and clamping to the minimum floor.
    /// </summary>
    /// <param name="state">The operator's current flow state.</param>
    /// <param name="baseDuration">The original animation duration.</param>
    /// <returns>
    /// Scaled duration, never less than <see cref="MinDurationMs"/> ms
    /// and never more than <paramref name="baseDuration"/>.
    /// </returns>
    public static TimeSpan GetAdaptedDuration(FlowState state, TimeSpan baseDuration)
    {
        if (baseDuration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var multiplier = GetDurationMultiplier(state);
        var adaptedMs = baseDuration.TotalMilliseconds * multiplier;
        var clampedMs = Math.Max(adaptedMs, MinDurationMs);

        // Never exceed the base duration
        clampedMs = Math.Min(clampedMs, baseDuration.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(clampedMs);
    }

    /// <summary>
    /// Convenience overload that accepts milliseconds directly.
    /// </summary>
    public static double GetAdaptedDurationMs(FlowState state, double baseDurationMs)
    {
        if (baseDurationMs <= 0)
            return 0;

        var multiplier = GetDurationMultiplier(state);
        var adaptedMs = baseDurationMs * multiplier;
        var clampedMs = Math.Max(adaptedMs, MinDurationMs);

        return Math.Min(clampedMs, baseDurationMs);
    }
}
