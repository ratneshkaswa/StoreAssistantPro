namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Immutable snapshot of a zero-click rule evaluation.
/// Returned by <see cref="IZeroClickRule.EvaluateAsync"/> so the
/// service can decide whether to auto-execute.
/// </summary>
public sealed record ZeroClickEvaluation(
    ZeroClickConfidence Confidence,
    string Description)
{
    /// <summary>Shortcut: no conditions met.</summary>
    public static ZeroClickEvaluation Skip(string reason) =>
        new(ZeroClickConfidence.None, reason);

    /// <summary>Shortcut: high confidence — will auto-execute.</summary>
    public static ZeroClickEvaluation AutoExecute(string description) =>
        new(ZeroClickConfidence.High, description);

    /// <summary>Shortcut: conditions met but blocked by safety gate.</summary>
    public static ZeroClickEvaluation Blocked(string reason) =>
        new(ZeroClickConfidence.Medium, reason);
}
