using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

public class FlowFocusAdapterTests
{
    // ═══════════════════════════════════════════════════════════════
    // GetIdleTimeoutMs
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetIdleTimeoutMs_Calm_Returns600()
    {
        Assert.Equal(600, FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Calm));
    }

    [Fact]
    public void GetIdleTimeoutMs_Focused_Returns400()
    {
        Assert.Equal(400, FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Focused));
    }

    [Fact]
    public void GetIdleTimeoutMs_Flow_Returns200()
    {
        Assert.Equal(200, FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Flow));
    }

    [Fact]
    public void IdleTimeout_MonotonicallyDecreases()
    {
        var calm = FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Calm);
        var focused = FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Focused);
        var flow = FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Flow);

        Assert.True(calm > focused, $"Calm ({calm}) should be > Focused ({focused})");
        Assert.True(focused > flow, $"Focused ({focused}) should be > Flow ({flow})");
    }

    // ═══════════════════════════════════════════════════════════════
    // GetPriorityBoost
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetPriorityBoost_Calm_ReturnsZero()
    {
        Assert.Equal(0, FlowFocusAdapter.GetPriorityBoost(FlowState.Calm));
    }

    [Fact]
    public void GetPriorityBoost_Focused_ReturnsFive()
    {
        Assert.Equal(5, FlowFocusAdapter.GetPriorityBoost(FlowState.Focused));
    }

    [Fact]
    public void GetPriorityBoost_Flow_ReturnsTen()
    {
        Assert.Equal(10, FlowFocusAdapter.GetPriorityBoost(FlowState.Flow));
    }

    [Fact]
    public void PriorityBoost_MonotonicallyIncreases()
    {
        var calm = FlowFocusAdapter.GetPriorityBoost(FlowState.Calm);
        var focused = FlowFocusAdapter.GetPriorityBoost(FlowState.Focused);
        var flow = FlowFocusAdapter.GetPriorityBoost(FlowState.Flow);

        Assert.True(calm < focused, $"Calm ({calm}) should be < Focused ({focused})");
        Assert.True(focused < flow, $"Focused ({focused}) should be < Flow ({flow})");
    }

    // ═══════════════════════════════════════════════════════════════
    // ShouldBypassInputGuard
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ShouldBypassInputGuard_Calm_ReturnsFalse()
    {
        Assert.False(FlowFocusAdapter.ShouldBypassInputGuard(FlowState.Calm));
    }

    [Fact]
    public void ShouldBypassInputGuard_Focused_ReturnsFalse()
    {
        Assert.False(FlowFocusAdapter.ShouldBypassInputGuard(FlowState.Focused));
    }

    [Fact]
    public void ShouldBypassInputGuard_Flow_ReturnsTrue()
    {
        Assert.True(FlowFocusAdapter.ShouldBypassInputGuard(FlowState.Flow));
    }

    // ═══════════════════════════════════════════════════════════════
    // Constants validation
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Constants_MatchDocumentation()
    {
        Assert.Equal(600, FlowFocusAdapter.CalmIdleTimeoutMs);
        Assert.Equal(400, FlowFocusAdapter.FocusedIdleTimeoutMs);
        Assert.Equal(200, FlowFocusAdapter.FlowIdleTimeoutMs);

        Assert.Equal(0, FlowFocusAdapter.CalmPriorityBoost);
        Assert.Equal(5, FlowFocusAdapter.FocusedPriorityBoost);
        Assert.Equal(10, FlowFocusAdapter.FlowPriorityBoost);
    }

    // ═══════════════════════════════════════════════════════════════
    // All states produce positive idle timeouts
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(FlowState.Calm)]
    [InlineData(FlowState.Focused)]
    [InlineData(FlowState.Flow)]
    public void GetIdleTimeoutMs_AllStates_Positive(FlowState state)
    {
        Assert.True(FlowFocusAdapter.GetIdleTimeoutMs(state) > 0);
    }

    [Theory]
    [InlineData(FlowState.Calm)]
    [InlineData(FlowState.Focused)]
    [InlineData(FlowState.Flow)]
    public void GetPriorityBoost_AllStates_NonNegative(FlowState state)
    {
        Assert.True(FlowFocusAdapter.GetPriorityBoost(state) >= 0);
    }
}
