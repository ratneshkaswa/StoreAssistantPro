using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Pure, static zero-click adaptation logic. Maps a <see cref="FlowState"/>
/// to execution parameters that <see cref="ZeroClickWorkflowService"/>
/// uses to adjust its aggressiveness.
///
/// <para><b>No DI, no UI, no side effects.</b> Every method is a
/// deterministic pure function.</para>
///
/// <para><b>Adaptation matrix:</b></para>
/// <code>
///   ┌──────────┬───────────────────┬──────────────────┬──────────────────────┐
///   │ State    │ AcceptMedium      │ AllowDataEntry   │ Effect               │
///   ├──────────┼───────────────────┼──────────────────┼──────────────────────┤
///   │ Calm     │ false             │ false            │ Conservative.        │
///   │ Focused  │ false             │ true             │ Standard.            │
///   │ Flow     │ true              │ true             │ Faster execution.    │
///   └──────────┴───────────────────┴──────────────────┴──────────────────────┘
/// </code>
///
/// <para><b>Confidence promotion (Flow only):</b></para>
/// <para>
/// In <see cref="FlowState.Flow"/>, rules that evaluate to
/// <see cref="ZeroClickConfidence.Medium"/> are promoted to
/// <see cref="ZeroClickConfidence.High"/> — enabling faster auto-execution
/// for scenarios where the operator is in rapid-fire mode and would
/// manually confirm anyway. This promotion is applied by
/// <see cref="AdaptConfidence"/> and only affects the execution gate, not
/// the rule's own evaluation logic.
/// </para>
///
/// <para><b>DataEntry gating (Calm):</b></para>
/// <para>
/// In <see cref="FlowState.Calm"/>, <see cref="ZeroClickActionCategory.DataEntry"/>
/// rules are blocked — the operator is browsing or configuring, and
/// automatic data entry (e.g., auto-add to cart) would be surprising.
/// In Focused and Flow states, DataEntry is allowed because the
/// operator is actively working in a billing session.
/// </para>
///
/// <para><b>Safety invariant:</b></para>
/// <para>
/// This adapter <b>never</b> overrides the <see cref="IZeroClickSafetyPolicy"/>
/// hardcoded blocklist. Destructive categories (Delete, SettingsChange,
/// FinancialConfirmation, SecuritySensitive) remain permanently blocked
/// regardless of flow state. The adapter only loosens restrictions for
/// safe categories in higher flow states.
/// </para>
/// </summary>
public static class FlowZeroClickAdapter
{
    /// <summary>
    /// Returns <c>true</c> when <see cref="ZeroClickConfidence.Medium"/>
    /// should be promoted to <see cref="ZeroClickConfidence.High"/> for
    /// the given flow state — enabling faster auto-execution.
    /// <para>Only <see cref="FlowState.Flow"/> enables this promotion.</para>
    /// </summary>
    public static bool ShouldAcceptMediumConfidence(FlowState state) =>
        state == FlowState.Flow;

    /// <summary>
    /// Returns <c>true</c> when <see cref="ZeroClickActionCategory.DataEntry"/>
    /// rules are allowed for the given flow state.
    /// <para>
    /// Calm → blocked (operator is browsing).
    /// Focused/Flow → allowed (operator is in an active billing session).
    /// </para>
    /// </summary>
    public static bool IsDataEntryAllowed(FlowState state) =>
        state != FlowState.Calm;

    /// <summary>
    /// Adapts a rule's evaluated confidence level based on the current
    /// flow state. In Flow mode, <see cref="ZeroClickConfidence.Medium"/>
    /// is promoted to <see cref="ZeroClickConfidence.High"/>.
    /// All other confidence levels are unchanged.
    /// </summary>
    /// <param name="state">The operator's current flow state.</param>
    /// <param name="original">The rule's original confidence evaluation.</param>
    /// <returns>
    /// The adapted confidence — same as <paramref name="original"/> unless
    /// the conditions for promotion are met.
    /// </returns>
    public static ZeroClickConfidence AdaptConfidence(FlowState state, ZeroClickConfidence original)
    {
        if (original == ZeroClickConfidence.Medium && ShouldAcceptMediumConfidence(state))
            return ZeroClickConfidence.High;

        return original;
    }

    /// <summary>
    /// Returns <c>true</c> when the given action category should be
    /// blocked for the given flow state. This is an <em>additional</em>
    /// gate on top of <see cref="IZeroClickSafetyPolicy"/> — it never
    /// allows categories that the safety policy blocks.
    /// </summary>
    /// <param name="state">The operator's current flow state.</param>
    /// <param name="category">The action category to check.</param>
    /// <returns>
    /// <c>true</c> when the category should be blocked in the current
    /// flow state (caller should skip the rule).
    /// </returns>
    public static bool IsCategoryBlockedByFlowState(FlowState state, ZeroClickActionCategory category)
    {
        if (category == ZeroClickActionCategory.DataEntry && !IsDataEntryAllowed(state))
            return true;

        return false;
    }
}
