namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Immutable result of an intent detection evaluation.
/// Contains the classified intent, confidence, the raw input, and
/// the context it was detected in.
/// </summary>
public sealed record IntentResult
{
    /// <summary>The classified intent type.</summary>
    public required InputIntent Intent { get; init; }

    /// <summary>
    /// Confidence score from 0.0 (no match) to 1.0 (certain match).
    /// Only intents with confidence ≥ <see cref="IIntentDetectionService.ConfidenceThreshold"/>
    /// are published as events.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>The raw input text that was classified.</summary>
    public required string RawInput { get; init; }

    /// <summary>The context in which the input was received.</summary>
    public required InputContext Context { get; init; }

    /// <summary>
    /// Optional payload — e.g., the matched product ID for
    /// <see cref="InputIntent.ExactProductMatch"/>, or the resolved
    /// barcode value for <see cref="InputIntent.BarcodeScan"/>.
    /// </summary>
    public string? ResolvedValue { get; init; }

    /// <summary>Shortcut: unknown intent with zero confidence.</summary>
    public static IntentResult None(string input, InputContext context) =>
        new()
        {
            Intent = InputIntent.Unknown,
            Confidence = 0.0,
            RawInput = input,
            Context = context
        };
}
