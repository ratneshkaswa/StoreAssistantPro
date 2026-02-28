namespace StoreAssistantPro.Models;

/// <summary>
/// A single rule verdict from the <see cref="Core.Services.FlowStateAnalyzer"/>.
/// Describes whether a specific interaction signal meets the threshold
/// for flow-state promotion.
/// </summary>
/// <param name="RuleName">
/// Short identifier for the rule (e.g., <c>"HighTypingSpeed"</c>,
/// <c>"LowIdle"</c>, <c>"RapidBillingActions"</c>).
/// </param>
/// <param name="Passed">
/// <c>true</c> when the rule's threshold condition is met.
/// </param>
/// <param name="Weight">
/// Contribution weight to the overall flow score (0.0–1.0).
/// Higher weight = more influence on the final recommendation.
/// </param>
/// <param name="Description">
/// Human-readable explanation of the evaluation
/// (e.g., <c>"Keyboard 4.2 keys/sec ≥ 2.0 threshold"</c>).
/// </param>
public sealed record FlowRule(
    string RuleName,
    bool Passed,
    double Weight,
    string Description);
