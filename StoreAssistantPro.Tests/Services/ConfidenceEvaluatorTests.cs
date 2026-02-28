using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Tests.Services;

public class ConfidenceEvaluatorTests
{
    // ── Helpers ──────────────────────────────────────────────────────

    private static IntentResult MakeIntent(
        InputIntent intent, double confidence,
        string input = "test", InputContext context = InputContext.BillingSearch,
        string? resolved = null) =>
        new()
        {
            Intent = intent,
            Confidence = confidence,
            RawInput = input,
            Context = context,
            ResolvedValue = resolved
        };

    // ═══════════════════════════════════════════════════════════════
    // Product Match
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ProductMatch_SingleResult_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.95, "Blue Shirt");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Equal(ZeroClickConfidence.High, verdict.Confidence);
        Assert.Equal(RejectionReason.None, verdict.Rejection);
        Assert.Equal(1, verdict.MatchCount);
    }

    [Fact]
    public void ProductMatch_MultipleResults_Rejects()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.95, "Shirt");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 5);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.MultipleMatches, verdict.Rejection);
        Assert.Equal(ZeroClickConfidence.Medium, verdict.Confidence);
        Assert.Equal(5, verdict.MatchCount);
    }

    [Fact]
    public void ProductMatch_ZeroResults_Rejects()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.95, "Nonexistent");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 0);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.AmbiguousInput, verdict.Rejection);
    }

    [Fact]
    public void ProductMatch_LowConfidence_Rejects()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.5, "Shirt");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.LowConfidence, verdict.Rejection);
    }

    [Fact]
    public void ProductMatch_WrongIntent_Rejects()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95);

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.InvalidContext, verdict.Rejection);
    }

    // ═══════════════════════════════════════════════════════════════
    // Barcode Scan
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void BarcodeScan_HighConfidence_SingleMatch_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95, "4006381333931");

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Equal(ZeroClickConfidence.High, verdict.Confidence);
        Assert.Equal(1, verdict.MatchCount);
    }

    [Fact]
    public void BarcodeScan_BelowThreshold_Rejects()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.8, "12345678");

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.LowConfidence, verdict.Rejection);
    }

    [Fact]
    public void BarcodeScan_ExactThreshold_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.9, "12345678");

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
    }

    [Fact]
    public void BarcodeScan_NoMatchInCatalog_Rejects()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95, "9999999999999");

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 0);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.AmbiguousInput, verdict.Rejection);
    }

    [Fact]
    public void BarcodeScan_MultipleMatches_DataIntegrityIssue_Rejects()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95, "4006381333931");

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 2);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.MultipleMatches, verdict.Rejection);
        Assert.Contains("integrity", verdict.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BarcodeScan_WrongIntent_Rejects()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 0.95);

        var verdict = ConfidenceEvaluator.EvaluateBarcodeScan(intent, matchCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.InvalidContext, verdict.Rejection);
    }

    // ═══════════════════════════════════════════════════════════════
    // PIN Completed
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void PinCompleted_4Digit_InPinContext_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 1.0, "4829",
            InputContext.PinEntry, "UserPin");

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Contains("UserPin", verdict.Explanation);
    }

    [Fact]
    public void PinCompleted_6Digit_InPinContext_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 1.0, "829417",
            InputContext.PinEntry, "MasterPin");

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Contains("MasterPin", verdict.Explanation);
    }

    [Fact]
    public void PinCompleted_WrongContext_Rejects()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 1.0, "4829",
            InputContext.General, "UserPin");

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.InvalidContext, verdict.Rejection);
    }

    [Fact]
    public void PinCompleted_NotCertain_Rejects()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 0.9, "4829",
            InputContext.PinEntry);

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.LowConfidence, verdict.Rejection);
    }

    [Fact]
    public void PinCompleted_WrongLength_Rejects()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 1.0, "12345",
            InputContext.PinEntry);

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.IncompleteInputs, verdict.Rejection);
    }

    [Fact]
    public void PinCompleted_WrongIntent_Rejects()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 1.0, "4829",
            InputContext.PinEntry);

        var verdict = ConfidenceEvaluator.EvaluatePinCompleted(intent);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.InvalidContext, verdict.Rejection);
    }

    // ═══════════════════════════════════════════════════════════════
    // Auto-Complete
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AutoComplete_SingleResult_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.AutoCompleteTrigger, 0.9, "shi",
            InputContext.ProductSearch);

        var verdict = ConfidenceEvaluator.EvaluateAutoComplete(intent, resultCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Equal(1, verdict.MatchCount);
    }

    [Fact]
    public void AutoComplete_MultipleResults_Rejects()
    {
        var intent = MakeIntent(InputIntent.AutoCompleteTrigger, 0.9, "sh",
            InputContext.ProductSearch);

        var verdict = ConfidenceEvaluator.EvaluateAutoComplete(intent, resultCount: 15);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.MultipleMatches, verdict.Rejection);
        Assert.Equal(15, verdict.MatchCount);
    }

    [Fact]
    public void AutoComplete_ZeroResults_Rejects()
    {
        var intent = MakeIntent(InputIntent.AutoCompleteTrigger, 0.9, "xyz",
            InputContext.ProductSearch);

        var verdict = ConfidenceEvaluator.EvaluateAutoComplete(intent, resultCount: 0);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.AmbiguousInput, verdict.Rejection);
    }

    [Fact]
    public void AutoComplete_LowConfidence_Rejects()
    {
        var intent = MakeIntent(InputIntent.AutoCompleteTrigger, 0.5, "ab",
            InputContext.General);

        var verdict = ConfidenceEvaluator.EvaluateAutoComplete(intent, resultCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.LowConfidence, verdict.Rejection);
    }

    // ═══════════════════════════════════════════════════════════════
    // General Evaluate (dispatch)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Evaluate_UnknownIntent_Rejects()
    {
        var intent = IntentResult.None("???", InputContext.General);

        var verdict = ConfidenceEvaluator.Evaluate(intent, matchCount: 0);

        Assert.False(verdict.ShouldAutoExecute);
        Assert.Equal(RejectionReason.UnknownIntent, verdict.Rejection);
        Assert.Equal(ZeroClickConfidence.None, verdict.Confidence);
    }

    [Fact]
    public void Evaluate_DispatchesToProductMatch()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.95, "Blue Shirt");

        var verdict = ConfidenceEvaluator.Evaluate(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
    }

    [Fact]
    public void Evaluate_DispatchesToBarcodeScan()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95, "4006381333931");

        var verdict = ConfidenceEvaluator.Evaluate(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
    }

    [Fact]
    public void Evaluate_DispatchesToPinCompleted()
    {
        var intent = MakeIntent(InputIntent.PinCompleted, 1.0, "4829",
            InputContext.PinEntry, "UserPin");

        var verdict = ConfidenceEvaluator.Evaluate(intent);

        Assert.True(verdict.ShouldAutoExecute);
    }

    [Fact]
    public void Evaluate_DispatchesToAutoComplete()
    {
        var intent = MakeIntent(InputIntent.AutoCompleteTrigger, 0.9, "shi",
            InputContext.ProductSearch);

        var verdict = ConfidenceEvaluator.Evaluate(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
    }

    // ═══════════════════════════════════════════════════════════════
    // ConfidenceVerdict record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Verdict_AutoExecute_SetsCorrectDefaults()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95);
        var verdict = ConfidenceVerdict.AutoExecute(intent, "test", 1);

        Assert.True(verdict.ShouldAutoExecute);
        Assert.Equal(ZeroClickConfidence.High, verdict.Confidence);
        Assert.Equal(RejectionReason.None, verdict.Rejection);
        Assert.Equal(1, verdict.MatchCount);
        Assert.Same(intent, verdict.Intent);
    }

    [Fact]
    public void Verdict_Reject_MultipleMatches_MapsToMedium()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95);
        var verdict = ConfidenceVerdict.Reject(
            intent, RejectionReason.MultipleMatches, "too many", 3);

        Assert.Equal(ZeroClickConfidence.Medium, verdict.Confidence);
    }

    [Fact]
    public void Verdict_Reject_AmbiguousInput_MapsToLow()
    {
        var intent = MakeIntent(InputIntent.BarcodeScan, 0.95);
        var verdict = ConfidenceVerdict.Reject(
            intent, RejectionReason.AmbiguousInput, "unclear");

        Assert.Equal(ZeroClickConfidence.Low, verdict.Confidence);
    }

    [Fact]
    public void Verdict_Reject_UnknownIntent_MapsToNone()
    {
        var intent = IntentResult.None("x", InputContext.General);
        var verdict = ConfidenceVerdict.Reject(
            intent, RejectionReason.UnknownIntent, "nope");

        Assert.Equal(ZeroClickConfidence.None, verdict.Confidence);
    }

    // ═══════════════════════════════════════════════════════════════
    // Constants
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MinIntentConfidence_IsPointSeven()
    {
        Assert.Equal(0.7, ConfidenceEvaluator.MinIntentConfidence);
    }

    [Fact]
    public void BarcodeAutoExecuteThreshold_IsPointNine()
    {
        Assert.Equal(0.9, ConfidenceEvaluator.BarcodeAutoExecuteThreshold);
    }

    // ═══════════════════════════════════════════════════════════════
    // Boundary: exact threshold values
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ProductMatch_ExactlyAtThreshold_AutoExecutes()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.7, "Shirt");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 1);

        Assert.True(verdict.ShouldAutoExecute);
    }

    [Fact]
    public void ProductMatch_JustBelowThreshold_Rejects()
    {
        var intent = MakeIntent(InputIntent.ExactProductMatch, 0.69, "Shirt");

        var verdict = ConfidenceEvaluator.EvaluateProductMatch(intent, matchCount: 1);

        Assert.False(verdict.ShouldAutoExecute);
    }
}
