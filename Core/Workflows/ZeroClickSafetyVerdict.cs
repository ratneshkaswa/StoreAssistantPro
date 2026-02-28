namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Immutable result of a <see cref="IZeroClickSafetyPolicy"/> evaluation.
/// </summary>
/// <param name="IsAllowed"><c>true</c> if zero-click execution is permitted.</param>
/// <param name="Category">The action category that was evaluated.</param>
/// <param name="Reason">Human-readable explanation of the decision.</param>
/// <param name="RuleId">The rule ID that was evaluated.</param>
public sealed record ZeroClickSafetyVerdict(
    bool IsAllowed,
    ZeroClickActionCategory Category,
    string Reason,
    string RuleId)
{
    /// <summary>Creates an "allowed" verdict.</summary>
    public static ZeroClickSafetyVerdict Allow(
        ZeroClickActionCategory category, string ruleId, string reason) =>
        new(true, category, reason, ruleId);

    /// <summary>Creates a "blocked" verdict.</summary>
    public static ZeroClickSafetyVerdict Block(
        ZeroClickActionCategory category, string ruleId, string reason) =>
        new(false, category, reason, ruleId);
}
