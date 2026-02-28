namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable result of <see cref="Core.Services.FlowStateAnalyzer.Analyze"/>.
/// Contains the recommended <see cref="FlowState"/>, a weighted confidence
/// score, and detailed per-rule verdicts.
///
/// <para><b>Scoring model:</b></para>
/// <code>
///   FlowScore = Σ (rule.Passed ? rule.Weight : 0) / Σ rule.Weight
///
///   FlowScore ≥ 0.6  →  Flow   (peak concentration)
///   FlowScore ≥ 0.3  →  Focused (moderate engagement)
///   FlowScore &lt; 0.3  →  Calm   (low urgency / idle)
/// </code>
/// </summary>
/// <param name="RecommendedState">
/// The flow state recommended by the interaction-metric analysis.
/// The <see cref="Core.Services.FlowStateEngine"/> may combine this
/// with mode/session/lock signals to make the final decision.
/// </param>
/// <param name="FlowScore">
/// Weighted score in [0.0, 1.0]. Higher = more flow-like.
/// </param>
/// <param name="Rules">
/// Detailed per-rule verdicts for diagnostics and logging.
/// </param>
/// <param name="Summary">
/// One-line human-readable summary of the analysis.
/// </param>
public sealed record FlowAnalysis(
    FlowState RecommendedState,
    double FlowScore,
    IReadOnlyList<FlowRule> Rules,
    string Summary)
{
    /// <summary>Flow-score threshold: at or above → <see cref="FlowState.Flow"/>.</summary>
    public const double FlowThreshold = 0.6;

    /// <summary>Flow-score threshold: at or above → <see cref="FlowState.Focused"/>.</summary>
    public const double FocusedThreshold = 0.3;
}
