using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Tests.Services;

public class ZeroClickSafetyPolicyTests
{
    private readonly ZeroClickSafetyPolicy _sut = new(
        NullLogger<ZeroClickSafetyPolicy>.Instance);

    // ═══════════════════════════════════════════════════════════════
    // Hardcoded blocked categories — NEVER allowed
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ZeroClickActionCategory.Delete)]
    [InlineData(ZeroClickActionCategory.SettingsChange)]
    [InlineData(ZeroClickActionCategory.FinancialConfirmation)]
    [InlineData(ZeroClickActionCategory.SecuritySensitive)]
    public void Evaluate_HardcodedBlocked_ReturnsBlocked(ZeroClickActionCategory category)
    {
        var verdict = _sut.Evaluate("TestRule", category);

        Assert.False(verdict.IsAllowed);
        Assert.Equal(category, verdict.Category);
        Assert.Equal("TestRule", verdict.RuleId);
    }

    [Theory]
    [InlineData(ZeroClickActionCategory.Delete)]
    [InlineData(ZeroClickActionCategory.SettingsChange)]
    [InlineData(ZeroClickActionCategory.FinancialConfirmation)]
    [InlineData(ZeroClickActionCategory.SecuritySensitive)]
    public void IsBlocked_HardcodedCategories_ReturnsTrue(ZeroClickActionCategory category)
    {
        Assert.True(_sut.IsBlocked(category));
    }

    [Fact]
    public void HardcodedBlockedCategories_ContainsAllFour()
    {
        var hardcoded = _sut.HardcodedBlockedCategories;

        Assert.Contains(ZeroClickActionCategory.Delete, hardcoded);
        Assert.Contains(ZeroClickActionCategory.SettingsChange, hardcoded);
        Assert.Contains(ZeroClickActionCategory.FinancialConfirmation, hardcoded);
        Assert.Contains(ZeroClickActionCategory.SecuritySensitive, hardcoded);
        Assert.Equal(4, hardcoded.Count);
    }

    // ═══════════════════════════════════════════════════════════════
    // Allowed categories
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ZeroClickActionCategory.ReadOnly)]
    [InlineData(ZeroClickActionCategory.Navigation)]
    [InlineData(ZeroClickActionCategory.DataEntry)]
    public void Evaluate_AllowedCategory_ReturnsAllowed(ZeroClickActionCategory category)
    {
        var verdict = _sut.Evaluate("TestRule", category);

        Assert.True(verdict.IsAllowed);
        Assert.Equal(category, verdict.Category);
    }

    [Theory]
    [InlineData(ZeroClickActionCategory.ReadOnly)]
    [InlineData(ZeroClickActionCategory.Navigation)]
    [InlineData(ZeroClickActionCategory.DataEntry)]
    public void IsBlocked_AllowedCategories_ReturnsFalse(ZeroClickActionCategory category)
    {
        Assert.False(_sut.IsBlocked(category));
    }

    // ═══════════════════════════════════════════════════════════════
    // Delete-specific rejection reasons
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Evaluate_Delete_ReasonMentionsDestructive()
    {
        var verdict = _sut.Evaluate("DeleteProduct", ZeroClickActionCategory.Delete);

        Assert.Contains("destructive", verdict.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_SettingsChange_ReasonMentionsConfiguration()
    {
        var verdict = _sut.Evaluate("ChangeTax", ZeroClickActionCategory.SettingsChange);

        Assert.Contains("configuration", verdict.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_FinancialConfirmation_ReasonMentionsMonetary()
    {
        var verdict = _sut.Evaluate("CompleteSale", ZeroClickActionCategory.FinancialConfirmation);

        Assert.Contains("monetary", verdict.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_SecuritySensitive_ReasonMentionsAccessControl()
    {
        var verdict = _sut.Evaluate("ChangePin", ZeroClickActionCategory.SecuritySensitive);

        Assert.Contains("access control", verdict.Reason, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════
    // Runtime blocking
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void BlockCategory_RuntimeBlocked_ReturnsBlocked()
    {
        _sut.BlockCategory(ZeroClickActionCategory.DataEntry);

        var verdict = _sut.Evaluate("AddToCart", ZeroClickActionCategory.DataEntry);

        Assert.False(verdict.IsAllowed);
    }

    [Fact]
    public void BlockCategory_AppearsInBlockedCategories()
    {
        _sut.BlockCategory(ZeroClickActionCategory.Navigation);

        Assert.Contains(ZeroClickActionCategory.Navigation, _sut.BlockedCategories);
    }

    [Fact]
    public void BlockCategory_IsBlocked_ReturnsTrue()
    {
        _sut.BlockCategory(ZeroClickActionCategory.ReadOnly);

        Assert.True(_sut.IsBlocked(ZeroClickActionCategory.ReadOnly));
    }

    // ═══════════════════════════════════════════════════════════════
    // Runtime unblocking
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UnblockCategory_RuntimeBlocked_RestoresAllowed()
    {
        _sut.BlockCategory(ZeroClickActionCategory.DataEntry);
        _sut.UnblockCategory(ZeroClickActionCategory.DataEntry);

        var verdict = _sut.Evaluate("AddToCart", ZeroClickActionCategory.DataEntry);

        Assert.True(verdict.IsAllowed);
    }

    [Fact]
    public void UnblockCategory_HardcodedBlocked_RemainsBlocked()
    {
        _sut.UnblockCategory(ZeroClickActionCategory.Delete);

        Assert.True(_sut.IsBlocked(ZeroClickActionCategory.Delete));
    }

    [Fact]
    public void UnblockCategory_HardcodedBlocked_EvaluateStillBlocks()
    {
        _sut.UnblockCategory(ZeroClickActionCategory.FinancialConfirmation);

        var verdict = _sut.Evaluate("Refund", ZeroClickActionCategory.FinancialConfirmation);

        Assert.False(verdict.IsAllowed);
    }

    // ═══════════════════════════════════════════════════════════════
    // BlockedCategories includes both hardcoded + runtime
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void BlockedCategories_IncludesHardcodedAndRuntime()
    {
        _sut.BlockCategory(ZeroClickActionCategory.Navigation);

        var blocked = _sut.BlockedCategories;

        // 4 hardcoded + 1 runtime
        Assert.Equal(5, blocked.Count);
        Assert.Contains(ZeroClickActionCategory.Delete, blocked);
        Assert.Contains(ZeroClickActionCategory.Navigation, blocked);
    }

    // ═══════════════════════════════════════════════════════════════
    // Verdict record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Verdict_Allow_Properties()
    {
        var v = ZeroClickSafetyVerdict.Allow(
            ZeroClickActionCategory.ReadOnly, "R1", "safe");

        Assert.True(v.IsAllowed);
        Assert.Equal(ZeroClickActionCategory.ReadOnly, v.Category);
        Assert.Equal("R1", v.RuleId);
        Assert.Equal("safe", v.Reason);
    }

    [Fact]
    public void Verdict_Block_Properties()
    {
        var v = ZeroClickSafetyVerdict.Block(
            ZeroClickActionCategory.Delete, "D1", "destructive");

        Assert.False(v.IsAllowed);
        Assert.Equal(ZeroClickActionCategory.Delete, v.Category);
        Assert.Equal("D1", v.RuleId);
    }

    [Fact]
    public void Verdict_RecordEquality()
    {
        var a = ZeroClickSafetyVerdict.Allow(ZeroClickActionCategory.ReadOnly, "R", "ok");
        var b = ZeroClickSafetyVerdict.Allow(ZeroClickActionCategory.ReadOnly, "R", "ok");

        Assert.Equal(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // ActionCategory enum
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ZeroClickActionCategory.ReadOnly)]
    [InlineData(ZeroClickActionCategory.Navigation)]
    [InlineData(ZeroClickActionCategory.DataEntry)]
    [InlineData(ZeroClickActionCategory.Delete)]
    [InlineData(ZeroClickActionCategory.SettingsChange)]
    [InlineData(ZeroClickActionCategory.FinancialConfirmation)]
    [InlineData(ZeroClickActionCategory.SecuritySensitive)]
    public void ActionCategory_AllValuesAreDefined(ZeroClickActionCategory category)
    {
        Assert.True(Enum.IsDefined(category));
    }

    // ═══════════════════════════════════════════════════════════════
    // IZeroClickRule.ActionCategory default
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void IZeroClickRule_ActionCategory_DefaultsToReadOnly()
    {
        var rule = NSubstitute.Substitute.For<IZeroClickRule>();

        // Default interface member returns ReadOnly
        Assert.Equal(ZeroClickActionCategory.ReadOnly, rule.ActionCategory);
    }

    // ═══════════════════════════════════════════════════════════════
    // Integration: blocked rule skipped by workflow service
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Evaluate_MultipleRules_MixedCategories()
    {
        var allowVerdict = _sut.Evaluate("SafeRule", ZeroClickActionCategory.Navigation);
        var blockVerdict = _sut.Evaluate("DangerousRule", ZeroClickActionCategory.Delete);

        Assert.True(allowVerdict.IsAllowed);
        Assert.False(blockVerdict.IsAllowed);
    }

    // ═══════════════════════════════════════════════════════════════
    // Edge cases
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void BlockCategory_AlreadyBlocked_NoError()
    {
        _sut.BlockCategory(ZeroClickActionCategory.DataEntry);
        _sut.BlockCategory(ZeroClickActionCategory.DataEntry); // idempotent

        Assert.True(_sut.IsBlocked(ZeroClickActionCategory.DataEntry));
    }

    [Fact]
    public void UnblockCategory_NotBlocked_NoError()
    {
        _sut.UnblockCategory(ZeroClickActionCategory.ReadOnly); // never was blocked

        Assert.False(_sut.IsBlocked(ZeroClickActionCategory.ReadOnly));
    }

    [Fact]
    public void BlockCategory_HardcodedCategory_NoError()
    {
        // Blocking a hardcoded category is redundant but harmless
        _sut.BlockCategory(ZeroClickActionCategory.Delete);

        Assert.True(_sut.IsBlocked(ZeroClickActionCategory.Delete));
    }
}
