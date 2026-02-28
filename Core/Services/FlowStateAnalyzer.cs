using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Pure, static flow-state analysis logic. Maps an
/// <see cref="InteractionSnapshot"/> to a <see cref="FlowAnalysis"/>
/// recommendation using weighted threshold rules.
///
/// <para>
/// <b>No DI, no UI, no side effects.</b> Every method is a deterministic
/// pure function. Follows the same design as
/// <see cref="Intents.ConfidenceEvaluator"/>.
/// </para>
///
/// <para><b>Rules (evaluated independently, then combined):</b></para>
/// <code>
///   ┌──────────────────────┬────────┬──────────────────────────────────┐
///   │ Rule                 │ Weight │ Condition                        │
///   ├──────────────────────┼────────┼──────────────────────────────────┤
///   │ HighTypingSpeed      │  0.35  │ KeyboardFrequency ≥ 2.0 keys/s  │
///   │ LowIdleTime          │  0.25  │ IdleSeconds &lt; 1.5 s             │
///   │ RapidBillingActions  │  0.25  │ BillingActionsPerMinute ≥ 20    │
///   │ SustainedMouseUse    │  0.10  │ MouseFrequency ≥ 0.5 moves/s   │
///   │ CombinedMomentum     │  0.05  │ IsRapidInput == true            │
///   └──────────────────────┴────────┴──────────────────────────────────┘
///
///   FlowScore = Σ passed weights / Σ all weights
///
///   Score ≥ 0.6 → Flow
///   Score ≥ 0.3 → Focused
///   Score &lt; 0.3 → Calm
/// </code>
///
/// <para><b>Idle override:</b> If <see cref="InteractionSnapshot.IsIdle"/>
/// is <c>true</c>, the analyzer short-circuits to <see cref="FlowState.Calm"/>
/// with score 0.0 — idle always wins.</para>
/// </summary>
public static class FlowStateAnalyzer
{
    // ── Thresholds ───────────────────────────────────────────────────

    /// <summary>Minimum keyboard frequency (keys/sec) for the typing rule.</summary>
    public const double TypingSpeedThreshold = 2.0;

    /// <summary>Maximum idle seconds for the low-idle rule.</summary>
    public const double IdleTimeThreshold = 1.5;

    /// <summary>Minimum billing actions/min for the rapid-billing rule.</summary>
    public const double BillingRateThreshold = 20.0;

    /// <summary>Minimum mouse frequency (moves/sec) for the mouse rule.</summary>
    public const double MouseFrequencyThreshold = 0.5;

    // ── Weights ──────────────────────────────────────────────────────

    /// <summary>Weight of the high-typing-speed rule.</summary>
    public const double TypingWeight = 0.35;

    /// <summary>Weight of the low-idle-time rule.</summary>
    public const double IdleWeight = 0.25;

    /// <summary>Weight of the rapid-billing-actions rule.</summary>
    public const double BillingWeight = 0.25;

    /// <summary>Weight of the sustained-mouse-use rule.</summary>
    public const double MouseWeight = 0.10;

    /// <summary>Weight of the combined-momentum rule.</summary>
    public const double MomentumWeight = 0.05;

    private const double TotalWeight =
        TypingWeight + IdleWeight + BillingWeight + MouseWeight + MomentumWeight;

    // ── Analysis ─────────────────────────────────────────────────────

    /// <summary>
    /// Analyzes an <see cref="InteractionSnapshot"/> and returns a
    /// <see cref="FlowAnalysis"/> with recommended state, score, and
    /// per-rule verdicts.
    /// </summary>
    public static FlowAnalysis Analyze(InteractionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // ── Idle short-circuit ───────────────────────────────────
        if (snapshot.IsIdle)
        {
            return new FlowAnalysis(
                FlowState.Calm,
                0.0,
                [
                    new FlowRule("IdleOverride", true, 1.0,
                        $"Idle {snapshot.IdleSeconds:F1}s ≥ 3.0s — short-circuit to Calm.")
                ],
                $"Idle ({snapshot.IdleSeconds:F1}s) — Calm.");
        }

        // ── Evaluate individual rules ────────────────────────────
        var rules = new List<FlowRule>(5);

        var typingPassed = snapshot.KeyboardFrequency >= TypingSpeedThreshold;
        rules.Add(new FlowRule(
            "HighTypingSpeed", typingPassed, TypingWeight,
            typingPassed
                ? $"Keyboard {snapshot.KeyboardFrequency:F1} keys/s ≥ {TypingSpeedThreshold:F1} threshold."
                : $"Keyboard {snapshot.KeyboardFrequency:F1} keys/s < {TypingSpeedThreshold:F1} threshold."));

        var idlePassed = snapshot.IdleSeconds < IdleTimeThreshold;
        rules.Add(new FlowRule(
            "LowIdleTime", idlePassed, IdleWeight,
            idlePassed
                ? $"Idle {snapshot.IdleSeconds:F1}s < {IdleTimeThreshold:F1}s threshold."
                : $"Idle {snapshot.IdleSeconds:F1}s ≥ {IdleTimeThreshold:F1}s threshold."));

        var billingPassed = snapshot.BillingActionsPerMinute >= BillingRateThreshold;
        rules.Add(new FlowRule(
            "RapidBillingActions", billingPassed, BillingWeight,
            billingPassed
                ? $"Billing {snapshot.BillingActionsPerMinute:F0} actions/min ≥ {BillingRateThreshold:F0} threshold."
                : $"Billing {snapshot.BillingActionsPerMinute:F0} actions/min < {BillingRateThreshold:F0} threshold."));

        var mousePassed = snapshot.MouseFrequency >= MouseFrequencyThreshold;
        rules.Add(new FlowRule(
            "SustainedMouseUse", mousePassed, MouseWeight,
            mousePassed
                ? $"Mouse {snapshot.MouseFrequency:F1} moves/s ≥ {MouseFrequencyThreshold:F1} threshold."
                : $"Mouse {snapshot.MouseFrequency:F1} moves/s < {MouseFrequencyThreshold:F1} threshold."));

        var momentumPassed = snapshot.IsRapidInput;
        rules.Add(new FlowRule(
            "CombinedMomentum", momentumPassed, MomentumWeight,
            momentumPassed
                ? "Combined momentum: rapid input detected (high keys/s + low idle)."
                : "Combined momentum: not in rapid input."));

        // ── Compute weighted score ───────────────────────────────
        var passedWeight = 0.0;
        foreach (var rule in rules)
        {
            if (rule.Passed)
                passedWeight += rule.Weight;
        }

        var score = Math.Round(passedWeight / TotalWeight, 3);

        // ── Map score to recommended state ───────────────────────
        var recommended = score switch
        {
            >= FlowAnalysis.FlowThreshold => FlowState.Flow,
            >= FlowAnalysis.FocusedThreshold => FlowState.Focused,
            _ => FlowState.Calm
        };

        var passedCount = 0;
        foreach (var rule in rules)
        {
            if (rule.Passed) passedCount++;
        }

        var summary = $"Score {score:F2} ({passedCount}/{rules.Count} rules passed) → {recommended}.";

        return new FlowAnalysis(recommended, score, rules, summary);
    }

    // ── Convenience queries ──────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the snapshot metrics indicate the
    /// operator is in a flow-like state (score ≥ <see cref="FlowAnalysis.FlowThreshold"/>).
    /// Lightweight alternative to full <see cref="Analyze"/> when
    /// only the boolean is needed.
    /// </summary>
    public static bool IsFlowLikely(InteractionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.IsIdle) return false;

        // Quick check: typing speed + low idle covers 60% weight,
        // which is enough to reach the flow threshold alone
        return snapshot.KeyboardFrequency >= TypingSpeedThreshold
            && snapshot.IdleSeconds < IdleTimeThreshold
            && snapshot.BillingActionsPerMinute >= BillingRateThreshold;
    }

    /// <summary>
    /// Returns <c>true</c> when the snapshot indicates the operator
    /// has gone idle — no meaningful input in recent history.
    /// </summary>
    public static bool IsIdleDetected(InteractionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.IsIdle;
    }
}
