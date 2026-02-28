namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Categorizes why a confidence evaluation was rejected.
/// Included in <see cref="ConfidenceVerdict"/> for diagnostics.
/// </summary>
public enum RejectionReason
{
    /// <summary>Not rejected — confidence is sufficient.</summary>
    None,

    /// <summary>Input matched multiple catalog entries.</summary>
    MultipleMatches,

    /// <summary>Input is ambiguous (partial match, mixed signals).</summary>
    AmbiguousInput,

    /// <summary>Intent was not recognized at all.</summary>
    UnknownIntent,

    /// <summary>Intent confidence fell below the auto-execute threshold.</summary>
    LowConfidence,

    /// <summary>Required inputs for the action are incomplete.</summary>
    IncompleteInputs,

    /// <summary>Context does not support auto-execution for this intent.</summary>
    InvalidContext
}
