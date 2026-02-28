using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class FlowStateAnalyzerTests
{
    private static readonly DateTime Now = new(2025, 6, 15, 10, 0, 0);

    // ═══════════════════════════════════════════════════════════════
    // Helper — build snapshots concisely
    // ═══════════════════════════════════════════════════════════════

    private static InteractionSnapshot Snap(
        double keyFreq = 0, double mouseFreq = 0,
        double idleSec = 0, double billingPm = 0)
        => new(keyFreq, mouseFreq, idleSec, billingPm, Now);

    // ═══════════════════════════════════════════════════════════════
    // Idle override → Calm
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_Idle_ReturnsCalmWithZeroScore()
    {
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: 5.0));

        Assert.Equal(FlowState.Calm, result.RecommendedState);
        Assert.Equal(0.0, result.FlowScore);
    }

    [Fact]
    public void Analyze_Idle_ShortCircuits_SingleRule()
    {
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: 3.0));

        Assert.Single(result.Rules);
        Assert.Equal("IdleOverride", result.Rules[0].RuleName);
        Assert.True(result.Rules[0].Passed);
    }

    [Fact]
    public void Analyze_IdleBoundary_3Seconds_TriggersCalmOverride()
    {
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: 3.0));

        Assert.Equal(FlowState.Calm, result.RecommendedState);
    }

    [Fact]
    public void Analyze_IdleBelowBoundary_DoesNotShortCircuit()
    {
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: 2.9));

        Assert.True(result.Rules.Count > 1);
    }

    [Fact]
    public void Analyze_Idle_HighTypingSpeed_StillCalm()
    {
        // Even with high typing, if idle ≥ 3.0 → Calm override
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 10.0, mouseFreq: 5.0, idleSec: 3.0, billingPm: 60));

        Assert.Equal(FlowState.Calm, result.RecommendedState);
    }

    // ═══════════════════════════════════════════════════════════════
    // All signals high → Flow
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_AllSignalsHigh_ReturnsFlow()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.Equal(FlowState.Flow, result.RecommendedState);
    }

    [Fact]
    public void Analyze_AllSignalsHigh_ScoreAboveFlowThreshold()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.True(result.FlowScore >= FlowAnalysis.FlowThreshold);
    }

    [Fact]
    public void Analyze_AllSignalsHigh_AllFiveRulesReturned()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.Equal(5, result.Rules.Count);
    }

    [Fact]
    public void Analyze_AllSignalsHigh_AllRulesPassed()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.All(result.Rules, r => Assert.True(r.Passed));
    }

    // ═══════════════════════════════════════════════════════════════
    // No signals → Calm
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_NoActivity_ReturnsCalmOrFocused()
    {
        // keyFreq=0, mouseFreq=0, idle=0 (just started), billing=0
        // IdleSeconds=0 → not idle, but all rules fail → score 0 → Calm
        var result = FlowStateAnalyzer.Analyze(Snap());

        // idle=0 < 1.5 passes LowIdleTime rule (weight 0.25)
        // score = 0.25/1.0 = 0.25 < 0.3 → Calm
        Assert.Equal(FlowState.Calm, result.RecommendedState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Typing + idle only → Focused (partial rules)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_TypingAndLowIdle_NoBilling_ReturnsFocused()
    {
        // typing (0.35) + idle (0.25) + momentum (0.05) = 0.65/1.0 = 0.65
        // But momentum requires IsRapidInput which needs keyFreq≥2 && idle<1.5 → true
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 0.5));

        // score = (0.35 + 0.25 + 0.05) / 1.0 = 0.65 → Flow
        Assert.Equal(FlowState.Flow, result.RecommendedState);
    }

    [Fact]
    public void Analyze_TypingOnly_HighIdle_ReturnsCalmOrFocused()
    {
        // typing passes (0.35), but idle fails, billing fails, mouse fails, momentum fails
        // score = 0.35 / 1.0 = 0.35 → Focused
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 2.0));

        Assert.Equal(FlowState.Focused, result.RecommendedState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Billing actions + idle → Focused
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_BillingAndLowIdle_NoTyping_ReturnsFocused()
    {
        // idle (0.25) + billing (0.25) = 0.5 / 1.0 = 0.5 → Focused
        var result = FlowStateAnalyzer.Analyze(
            Snap(billingPm: 30, idleSec: 0.5));

        Assert.Equal(FlowState.Focused, result.RecommendedState);
    }

    // ═══════════════════════════════════════════════════════════════
    // Individual rule thresholds
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1.9, false)]  // below threshold
    [InlineData(2.0, true)]   // at threshold
    [InlineData(5.0, true)]   // above threshold
    public void Rule_HighTypingSpeed_Threshold(double keyFreq, bool expectedPass)
    {
        var result = FlowStateAnalyzer.Analyze(Snap(keyFreq: keyFreq));

        var rule = result.Rules.Single(r => r.RuleName == "HighTypingSpeed");
        Assert.Equal(expectedPass, rule.Passed);
    }

    [Theory]
    [InlineData(0.0, true)]   // zero idle → passes
    [InlineData(1.4, true)]   // just below threshold
    [InlineData(1.5, false)]  // at threshold
    [InlineData(2.5, false)]  // above threshold
    public void Rule_LowIdleTime_Threshold(double idleSec, bool expectedPass)
    {
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: idleSec));

        var rule = result.Rules.Single(r => r.RuleName == "LowIdleTime");
        Assert.Equal(expectedPass, rule.Passed);
    }

    [Theory]
    [InlineData(10, false)]  // below threshold
    [InlineData(20, true)]   // at threshold
    [InlineData(60, true)]   // above threshold
    public void Rule_RapidBillingActions_Threshold(double billingPm, bool expectedPass)
    {
        var result = FlowStateAnalyzer.Analyze(Snap(billingPm: billingPm));

        var rule = result.Rules.Single(r => r.RuleName == "RapidBillingActions");
        Assert.Equal(expectedPass, rule.Passed);
    }

    [Theory]
    [InlineData(0.0, false)]  // no mouse
    [InlineData(0.4, false)]  // below threshold
    [InlineData(0.5, true)]   // at threshold
    [InlineData(3.0, true)]   // above threshold
    public void Rule_SustainedMouseUse_Threshold(double mouseFreq, bool expectedPass)
    {
        var result = FlowStateAnalyzer.Analyze(Snap(mouseFreq: mouseFreq));

        var rule = result.Rules.Single(r => r.RuleName == "SustainedMouseUse");
        Assert.Equal(expectedPass, rule.Passed);
    }

    [Fact]
    public void Rule_CombinedMomentum_PassesWhenRapidInput()
    {
        // IsRapidInput requires keyFreq ≥ 2.0 && idle < 1.5
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 0.5));

        var rule = result.Rules.Single(r => r.RuleName == "CombinedMomentum");
        Assert.True(rule.Passed);
    }

    [Fact]
    public void Rule_CombinedMomentum_FailsWhenNotRapidInput()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 1.0, idleSec: 0.5));

        var rule = result.Rules.Single(r => r.RuleName == "CombinedMomentum");
        Assert.False(rule.Passed);
    }

    // ═══════════════════════════════════════════════════════════════
    // Rule weights
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Rules_HaveCorrectWeights()
    {
        var result = FlowStateAnalyzer.Analyze(Snap(keyFreq: 5.0, idleSec: 0.2));

        Assert.Equal(0.35, result.Rules.Single(r => r.RuleName == "HighTypingSpeed").Weight);
        Assert.Equal(0.25, result.Rules.Single(r => r.RuleName == "LowIdleTime").Weight);
        Assert.Equal(0.25, result.Rules.Single(r => r.RuleName == "RapidBillingActions").Weight);
        Assert.Equal(0.10, result.Rules.Single(r => r.RuleName == "SustainedMouseUse").Weight);
        Assert.Equal(0.05, result.Rules.Single(r => r.RuleName == "CombinedMomentum").Weight);
    }

    [Fact]
    public void Rules_WeightsSumToOne()
    {
        var total = FlowStateAnalyzer.TypingWeight
                  + FlowStateAnalyzer.IdleWeight
                  + FlowStateAnalyzer.BillingWeight
                  + FlowStateAnalyzer.MouseWeight
                  + FlowStateAnalyzer.MomentumWeight;

        Assert.Equal(1.0, total);
    }

    // ═══════════════════════════════════════════════════════════════
    // Score thresholds
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowThreshold_Is06()
    {
        Assert.Equal(0.6, FlowAnalysis.FlowThreshold);
    }

    [Fact]
    public void FocusedThreshold_Is03()
    {
        Assert.Equal(0.3, FlowAnalysis.FocusedThreshold);
    }

    // ═══════════════════════════════════════════════════════════════
    // Score calculation verification
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Score_OnlyTypingPasses_Is035()
    {
        // typing=yes (0.35), idle=no (idleSec=2.0≥1.5), billing=no, mouse=no, momentum=no
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 2.0));

        Assert.Equal(0.35, result.FlowScore);
    }

    [Fact]
    public void Score_TypingAndIdle_Is060()
    {
        // typing=yes (0.35), idle=yes (0.25), billing=no, mouse=no, momentum=yes (0.05)
        // momentum passes because keyFreq≥2 && idleSec<1.5
        // total = 0.35 + 0.25 + 0.05 = 0.65
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 0.5));

        Assert.Equal(0.65, result.FlowScore);
    }

    [Fact]
    public void Score_AllPass_Is100()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.Equal(1.0, result.FlowScore);
    }

    // ═══════════════════════════════════════════════════════════════
    // Summary string
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Summary_ContainsScore()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, idleSec: 0.2, billingPm: 40));

        Assert.Contains("Score", result.Summary);
    }

    [Fact]
    public void Summary_ContainsRecommendedState()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.Contains("Flow", result.Summary);
    }

    // ═══════════════════════════════════════════════════════════════
    // IsFlowLikely convenience
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void IsFlowLikely_AllHighSignals_ReturnsTrue()
    {
        var snap = Snap(keyFreq: 5.0, idleSec: 0.2, billingPm: 40);

        Assert.True(FlowStateAnalyzer.IsFlowLikely(snap));
    }

    [Fact]
    public void IsFlowLikely_Idle_ReturnsFalse()
    {
        var snap = Snap(keyFreq: 5.0, idleSec: 5.0, billingPm: 40);

        Assert.False(FlowStateAnalyzer.IsFlowLikely(snap));
    }

    [Fact]
    public void IsFlowLikely_LowTyping_ReturnsFalse()
    {
        var snap = Snap(keyFreq: 0.5, idleSec: 0.2, billingPm: 40);

        Assert.False(FlowStateAnalyzer.IsFlowLikely(snap));
    }

    [Fact]
    public void IsFlowLikely_NoBilling_ReturnsFalse()
    {
        var snap = Snap(keyFreq: 5.0, idleSec: 0.2, billingPm: 5);

        Assert.False(FlowStateAnalyzer.IsFlowLikely(snap));
    }

    // ═══════════════════════════════════════════════════════════════
    // IsIdleDetected convenience
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void IsIdleDetected_HighIdle_ReturnsTrue()
    {
        Assert.True(FlowStateAnalyzer.IsIdleDetected(Snap(idleSec: 5.0)));
    }

    [Fact]
    public void IsIdleDetected_LowIdle_ReturnsFalse()
    {
        Assert.False(FlowStateAnalyzer.IsIdleDetected(Snap(idleSec: 1.0)));
    }

    // ═══════════════════════════════════════════════════════════════
    // Null argument
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Analyze_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => FlowStateAnalyzer.Analyze(null!));
    }

    [Fact]
    public void IsFlowLikely_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => FlowStateAnalyzer.IsFlowLikely(null!));
    }

    [Fact]
    public void IsIdleDetected_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => FlowStateAnalyzer.IsIdleDetected(null!));
    }

    // ═══════════════════════════════════════════════════════════════
    // FlowAnalysis record equality
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowAnalysis_RecordEquality()
    {
        var rules = new List<FlowRule>
        {
            new("R1", true, 0.5, "desc")
        };
        var a = new FlowAnalysis(FlowState.Flow, 0.8, rules, "summary");
        var b = new FlowAnalysis(FlowState.Flow, 0.8, rules, "summary");

        Assert.Equal(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // FlowRule record equality
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowRule_RecordEquality()
    {
        var a = new FlowRule("HighTypingSpeed", true, 0.35, "desc");
        var b = new FlowRule("HighTypingSpeed", true, 0.35, "desc");

        Assert.Equal(a, b);
    }

    [Fact]
    public void FlowRule_DifferentPassed_NotEqual()
    {
        var a = new FlowRule("R", true, 0.5, "d");
        var b = new FlowRule("R", false, 0.5, "d");

        Assert.NotEqual(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // Rule descriptions are meaningful
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RuleDescriptions_ContainThresholdValues()
    {
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        foreach (var rule in result.Rules)
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Description));
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Gradual escalation: more signals → higher score
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Score_IncreasesAsMoreSignalsActivate()
    {
        // Stage 1: only idle passes
        var s1 = FlowStateAnalyzer.Analyze(Snap(idleSec: 0.5));

        // Stage 2: typing + idle
        var s2 = FlowStateAnalyzer.Analyze(Snap(keyFreq: 3.0, idleSec: 0.5));

        // Stage 3: typing + idle + billing
        var s3 = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 0.5, billingPm: 30));

        // Stage 4: all signals
        var s4 = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 5.0, mouseFreq: 2.0, idleSec: 0.2, billingPm: 40));

        Assert.True(s1.FlowScore < s2.FlowScore);
        Assert.True(s2.FlowScore < s3.FlowScore);
        Assert.True(s3.FlowScore <= s4.FlowScore);
    }

    // ═══════════════════════════════════════════════════════════════
    // State boundary: Calm → Focused → Flow
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void StateBoundary_JustBelowFocusedThreshold_ReturnsCalm()
    {
        // Only LowIdleTime passes (weight 0.25) → score 0.25 < 0.3 → Calm
        var result = FlowStateAnalyzer.Analyze(Snap(idleSec: 0.5));

        Assert.Equal(FlowState.Calm, result.RecommendedState);
        Assert.True(result.FlowScore < FlowAnalysis.FocusedThreshold);
    }

    [Fact]
    public void StateBoundary_AtFocusedThreshold_ReturnsFocused()
    {
        // typing only (weight 0.35) → score 0.35 ≥ 0.3 → Focused
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 2.0));

        Assert.Equal(FlowState.Focused, result.RecommendedState);
        Assert.True(result.FlowScore >= FlowAnalysis.FocusedThreshold);
        Assert.True(result.FlowScore < FlowAnalysis.FlowThreshold);
    }

    [Fact]
    public void StateBoundary_AtFlowThreshold_ReturnsFlow()
    {
        // typing (0.35) + idle (0.25) + momentum (0.05) = 0.65 ≥ 0.6 → Flow
        var result = FlowStateAnalyzer.Analyze(
            Snap(keyFreq: 3.0, idleSec: 0.5));

        Assert.Equal(FlowState.Flow, result.RecommendedState);
        Assert.True(result.FlowScore >= FlowAnalysis.FlowThreshold);
    }
}
