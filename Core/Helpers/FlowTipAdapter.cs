using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Pure, static tip-adaptation logic. Maps a <see cref="FlowState"/>
/// to a <see cref="TipVisibilityPolicy"/> that the tip rotation
/// service uses to gate tip display.
///
/// <para><b>No DI, no UI, no side effects.</b> Every method is a
/// deterministic pure function.</para>
///
/// <para><b>Policy matrix:</b></para>
/// <code>
///   ┌──────────┬─────────────────┬─────────────────────────────────────┐
///   │ State    │ Policy          │ Effect                              │
///   ├──────────┼─────────────────┼─────────────────────────────────────┤
///   │ Calm     │ Normal          │ Tips shown per normal rotation.     │
///   │ Focused  │ Reduced         │ Cooldown multiplied by 3×.          │
///   │ Flow     │ Hidden          │ All tips suppressed.                │
///   └──────────┴─────────────────┴─────────────────────────────────────┘
/// </code>
///
/// <para><b>Cooldown multiplier:</b></para>
/// <para>
/// In <see cref="TipVisibilityPolicy.Reduced"/> mode, the effective
/// cooldown between tip displays is multiplied by
/// <see cref="FocusedCooldownMultiplier"/> (3×). This means a Beginner
/// operator who normally sees tips on every visit will instead see
/// them every 3rd visit; an Intermediate operator with a 5 min
/// cooldown will effectively wait 15 min.
/// </para>
///
/// <para><b>Integration:</b>
/// <see cref="TipRotationService.GetNextTip"/> calls
/// <see cref="GetPolicy"/> and <see cref="GetEffectiveCooldown"/>
/// to adjust behavior without changing the core rotation algorithm.
/// </para>
/// </summary>
public static class FlowTipAdapter
{
    /// <summary>Cooldown multiplier applied during Focused state.</summary>
    public const int FocusedCooldownMultiplier = 3;

    /// <summary>
    /// Returns the tip visibility policy for the given flow state.
    /// </summary>
    public static TipVisibilityPolicy GetPolicy(FlowState state) => state switch
    {
        FlowState.Flow => TipVisibilityPolicy.Hidden,
        FlowState.Focused => TipVisibilityPolicy.Reduced,
        _ => TipVisibilityPolicy.Normal
    };

    /// <summary>
    /// Adapts a base cooldown by applying the flow-state policy.
    /// <para>
    /// <see cref="TipVisibilityPolicy.Normal"/> → base cooldown unchanged.
    /// <see cref="TipVisibilityPolicy.Reduced"/> → base × <see cref="FocusedCooldownMultiplier"/>.
    /// <see cref="TipVisibilityPolicy.Hidden"/> → <see cref="TimeSpan.MaxValue"/> (infinite).
    /// </para>
    /// </summary>
    /// <param name="state">The operator's current flow state.</param>
    /// <param name="baseCooldown">The experience-profile cooldown.</param>
    /// <returns>Effective cooldown to use for tip gating.</returns>
    public static TimeSpan GetEffectiveCooldown(FlowState state, TimeSpan baseCooldown)
    {
        var policy = GetPolicy(state);
        return policy switch
        {
            TipVisibilityPolicy.Hidden => TimeSpan.MaxValue,
            TipVisibilityPolicy.Reduced => baseCooldown.TotalMilliseconds > 0
                ? TimeSpan.FromMilliseconds(baseCooldown.TotalMilliseconds * FocusedCooldownMultiplier)
                : TimeSpan.FromMinutes(1), // Beginners normally have zero cooldown; in Focused, give them 1 min
            _ => baseCooldown
        };
    }
}

/// <summary>
/// Determines how aggressively tips are displayed based on the
/// operator's <see cref="FlowState"/>.
/// </summary>
public enum TipVisibilityPolicy
{
    /// <summary>Normal tip rotation — no flow-based suppression.</summary>
    Normal,

    /// <summary>Reduced frequency — cooldown multiplied.</summary>
    Reduced,

    /// <summary>All tips hidden — peak concentration.</summary>
    Hidden
}
