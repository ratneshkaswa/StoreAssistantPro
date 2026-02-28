using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Immutable result of a <see cref="ConfidenceEvaluator"/> assessment.
/// Bridges <see cref="IntentResult"/> to <see cref="ZeroClickConfidence"/>
/// with an explanation and optional rejection reason.
/// </summary>
public sealed record ConfidenceVerdict
{
    /// <summary>Whether the action should auto-execute.</summary>
    public required bool ShouldAutoExecute { get; init; }

    /// <summary>Mapped confidence level for the zero-click pipeline.</summary>
    public required ZeroClickConfidence Confidence { get; init; }

    /// <summary>Human-readable explanation of the decision.</summary>
    public required string Explanation { get; init; }

    /// <summary>Why the evaluation was rejected, or <see cref="RejectionReason.None"/>.</summary>
    public required RejectionReason Rejection { get; init; }

    /// <summary>The original intent result that was evaluated.</summary>
    public required IntentResult Intent { get; init; }

    /// <summary>Number of catalog matches found (for product/barcode intents).</summary>
    public int MatchCount { get; init; }

    /// <summary>Creates an auto-execute verdict.</summary>
    public static ConfidenceVerdict AutoExecute(
        IntentResult intent, string explanation, int matchCount = 1) =>
        new()
        {
            ShouldAutoExecute = true,
            Confidence = ZeroClickConfidence.High,
            Explanation = explanation,
            Rejection = RejectionReason.None,
            Intent = intent,
            MatchCount = matchCount
        };

    /// <summary>Creates a rejection verdict.</summary>
    public static ConfidenceVerdict Reject(
        IntentResult intent, RejectionReason reason, string explanation,
        int matchCount = 0) =>
        new()
        {
            ShouldAutoExecute = false,
            Confidence = reason switch
            {
                RejectionReason.MultipleMatches => ZeroClickConfidence.Medium,
                RejectionReason.AmbiguousInput => ZeroClickConfidence.Low,
                RejectionReason.LowConfidence => ZeroClickConfidence.Low,
                _ => ZeroClickConfidence.None
            },
            Explanation = explanation,
            Rejection = reason,
            Intent = intent,
            MatchCount = matchCount
        };
}
