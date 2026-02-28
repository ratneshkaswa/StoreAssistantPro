namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Pure, static confidence evaluation logic that decides whether an
/// intent should trigger automatic execution.
/// <para>
/// <b>No DI, no UI, no side effects.</b> All methods are deterministic
/// pure functions that map <see cref="IntentResult"/> + match metadata
/// to a <see cref="ConfidenceVerdict"/>.
/// </para>
///
/// <para><b>Auto-execute rules (all must hold):</b></para>
/// <list type="bullet">
///   <item>Exact product match with single catalog result.</item>
///   <item>Single search result (unambiguous).</item>
///   <item>Valid barcode detected with ≥ 0.9 confidence.</item>
///   <item>PIN completed with correct digit count in PIN context.</item>
/// </list>
///
/// <para><b>Rejection rules (any triggers reject):</b></para>
/// <list type="bullet">
///   <item>Multiple catalog matches for the same input.</item>
///   <item>Ambiguous input (low confidence or mixed signals).</item>
///   <item>Unknown intent.</item>
///   <item>Incomplete required inputs.</item>
/// </list>
/// </summary>
public static class ConfidenceEvaluator
{
    /// <summary>
    /// Minimum intent confidence to even consider auto-execution.
    /// Below this, the verdict is always reject.
    /// </summary>
    public const double MinIntentConfidence = 0.7;

    /// <summary>
    /// Barcode confidence threshold for auto-add-to-cart.
    /// EAN-13 with valid check digit reaches 0.95; anything below
    /// 0.9 is treated as uncertain.
    /// </summary>
    public const double BarcodeAutoExecuteThreshold = 0.9;

    /// <summary>
    /// Evaluates an exact product match intent.
    /// Auto-executes only when exactly one product matched.
    /// </summary>
    /// <param name="intent">The classified intent (should be <see cref="InputIntent.ExactProductMatch"/>).</param>
    /// <param name="matchCount">Number of catalog products that matched the input.</param>
    public static ConfidenceVerdict EvaluateProductMatch(
        IntentResult intent, int matchCount)
    {
        if (intent.Intent != InputIntent.ExactProductMatch)
            return ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                $"Expected ExactProductMatch intent, got {intent.Intent}.");

        if (intent.Confidence < MinIntentConfidence)
            return ConfidenceVerdict.Reject(intent, RejectionReason.LowConfidence,
                $"Intent confidence {intent.Confidence:P0} is below threshold {MinIntentConfidence:P0}.",
                matchCount);

        return matchCount switch
        {
            0 => ConfidenceVerdict.Reject(intent, RejectionReason.AmbiguousInput,
                    "No products matched the input.", 0),

            1 => ConfidenceVerdict.AutoExecute(intent,
                    $"Exact match: single product found for '{intent.RawInput}'.", 1),

            _ => ConfidenceVerdict.Reject(intent, RejectionReason.MultipleMatches,
                    $"Ambiguous: {matchCount} products matched '{intent.RawInput}'.",
                    matchCount)
        };
    }

    /// <summary>
    /// Evaluates a barcode scan intent.
    /// Auto-executes when barcode confidence ≥ <see cref="BarcodeAutoExecuteThreshold"/>
    /// and exactly one product is associated with the barcode.
    /// </summary>
    /// <param name="intent">The classified intent (should be <see cref="InputIntent.BarcodeScan"/>).</param>
    /// <param name="matchCount">Number of products with this barcode in the catalog.</param>
    public static ConfidenceVerdict EvaluateBarcodeScan(
        IntentResult intent, int matchCount)
    {
        if (intent.Intent != InputIntent.BarcodeScan)
            return ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                $"Expected BarcodeScan intent, got {intent.Intent}.");

        if (intent.Confidence < BarcodeAutoExecuteThreshold)
            return ConfidenceVerdict.Reject(intent, RejectionReason.LowConfidence,
                $"Barcode confidence {intent.Confidence:P0} is below auto-execute threshold {BarcodeAutoExecuteThreshold:P0}.",
                matchCount);

        return matchCount switch
        {
            0 => ConfidenceVerdict.Reject(intent, RejectionReason.AmbiguousInput,
                    $"Barcode '{intent.RawInput}' not found in catalog.", 0),

            1 => ConfidenceVerdict.AutoExecute(intent,
                    $"Valid barcode '{intent.RawInput}' matched single product.", 1),

            _ => ConfidenceVerdict.Reject(intent, RejectionReason.MultipleMatches,
                    $"Barcode '{intent.RawInput}' matched {matchCount} products — data integrity issue.",
                    matchCount)
        };
    }

    /// <summary>
    /// Evaluates a completed PIN entry.
    /// Auto-executes when the PIN has the correct digit count
    /// for its resolved type (4-digit user or 6-digit master).
    /// </summary>
    /// <param name="intent">The classified intent (should be <see cref="InputIntent.PinCompleted"/>).</param>
    public static ConfidenceVerdict EvaluatePinCompleted(IntentResult intent)
    {
        if (intent.Intent != InputIntent.PinCompleted)
            return ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                $"Expected PinCompleted intent, got {intent.Intent}.");

        if (intent.Context != InputContext.PinEntry)
            return ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                "PIN completed outside of PinEntry context.");

        if (intent.Confidence < 1.0)
            return ConfidenceVerdict.Reject(intent, RejectionReason.LowConfidence,
                $"PIN confidence {intent.Confidence:P0} is not certain.");

        var isValidLength = intent.RawInput.Length is 4 or 6;
        if (!isValidLength)
            return ConfidenceVerdict.Reject(intent, RejectionReason.IncompleteInputs,
                $"PIN length {intent.RawInput.Length} is not 4 or 6 digits.");

        var pinType = intent.ResolvedValue ?? (intent.RawInput.Length == 4 ? "UserPin" : "MasterPin");
        return ConfidenceVerdict.AutoExecute(intent,
            $"{pinType} entry complete — auto-submit.", 1);
    }

    /// <summary>
    /// Evaluates an auto-complete trigger.
    /// Auto-executes (shows suggestions) when exactly one search
    /// result exists; otherwise suggests but does not auto-select.
    /// </summary>
    /// <param name="intent">The classified intent (should be <see cref="InputIntent.AutoCompleteTrigger"/>).</param>
    /// <param name="resultCount">Number of search results returned.</param>
    public static ConfidenceVerdict EvaluateAutoComplete(
        IntentResult intent, int resultCount)
    {
        if (intent.Intent != InputIntent.AutoCompleteTrigger)
            return ConfidenceVerdict.Reject(intent, RejectionReason.InvalidContext,
                $"Expected AutoCompleteTrigger intent, got {intent.Intent}.");

        if (intent.Confidence < MinIntentConfidence)
            return ConfidenceVerdict.Reject(intent, RejectionReason.LowConfidence,
                $"Auto-complete confidence {intent.Confidence:P0} is below threshold.",
                resultCount);

        return resultCount switch
        {
            0 => ConfidenceVerdict.Reject(intent, RejectionReason.AmbiguousInput,
                    "No results to auto-complete.", 0),

            1 => ConfidenceVerdict.AutoExecute(intent,
                    $"Single result for '{intent.RawInput}' — auto-select.", 1),

            _ => ConfidenceVerdict.Reject(intent, RejectionReason.MultipleMatches,
                    $"{resultCount} results for '{intent.RawInput}' — show suggestions, don't auto-select.",
                    resultCount)
        };
    }

    /// <summary>
    /// General-purpose evaluator that dispatches to the appropriate
    /// intent-specific method. Use when the caller doesn't know the
    /// intent type in advance.
    /// </summary>
    /// <param name="intent">The classified intent.</param>
    /// <param name="matchCount">
    /// Number of catalog/search matches. Interpretation depends on intent:
    /// products for barcode/product match, search results for auto-complete.
    /// </param>
    public static ConfidenceVerdict Evaluate(IntentResult intent, int matchCount = 0)
    {
        if (intent.Intent == InputIntent.Unknown)
            return ConfidenceVerdict.Reject(intent, RejectionReason.UnknownIntent,
                "Unrecognized input — no action taken.");

        return intent.Intent switch
        {
            InputIntent.ExactProductMatch => EvaluateProductMatch(intent, matchCount),
            InputIntent.BarcodeScan => EvaluateBarcodeScan(intent, matchCount),
            InputIntent.PinCompleted => EvaluatePinCompleted(intent),
            InputIntent.AutoCompleteTrigger => EvaluateAutoComplete(intent, matchCount),
            _ => ConfidenceVerdict.Reject(intent, RejectionReason.UnknownIntent,
                    $"No evaluator for intent {intent.Intent}.")
        };
    }
}
