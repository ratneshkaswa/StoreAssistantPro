namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Immutable result of <see cref="ISmartEnterKeyService.Evaluate"/>.
/// Tells <see cref="Helpers.KeyboardNav"/> what to do with an Enter press.
/// </summary>
public sealed record EnterKeyDecision
{
    /// <summary>The recommended action.</summary>
    public required EnterKeyAction Action { get; init; }

    /// <summary>
    /// When <see cref="Action"/> is <see cref="EnterKeyAction.Execute"/>,
    /// identifies the action that was triggered (for logging/diagnostics).
    /// Empty for <see cref="EnterKeyAction.MoveNext"/> and
    /// <see cref="EnterKeyAction.Suppress"/>.
    /// </summary>
    public string ActionId { get; init; } = string.Empty;

    /// <summary>Human-readable explanation for diagnostics.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Standard navigation — move to next field.</summary>
    public static EnterKeyDecision MoveNext(string reason = "No high-confidence action") =>
        new() { Action = EnterKeyAction.MoveNext, Reason = reason };

    /// <summary>Auto-execute a high-confidence action.</summary>
    public static EnterKeyDecision Execute(string actionId, string reason) =>
        new() { Action = EnterKeyAction.Execute, ActionId = actionId, Reason = reason };

    /// <summary>Suppress — previous action still running.</summary>
    public static EnterKeyDecision Suppress(string reason = "Previous action in progress") =>
        new() { Action = EnterKeyAction.Suppress, Reason = reason };
}
