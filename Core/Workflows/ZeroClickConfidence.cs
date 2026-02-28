namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Confidence level assigned to a zero-click rule evaluation.
/// Only <see cref="High"/> triggers automatic execution.
/// </summary>
public enum ZeroClickConfidence
{
    /// <summary>Rule conditions not met. No action taken.</summary>
    None,

    /// <summary>Conditions partially met. Action suggested but not executed.</summary>
    Low,

    /// <summary>Conditions met but safety gate prevents execution.</summary>
    Medium,

    /// <summary>Conditions fully met and all safety gates passed. Action executes automatically.</summary>
    High
}
