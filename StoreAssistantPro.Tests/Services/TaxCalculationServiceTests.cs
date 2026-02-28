using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class TaxCalculationServiceTests
{
    private readonly ITaxCalculationService _sut = new TaxCalculationService();

    // ── Intra-state (CGST + SGST) ──────────────────────────────────

    [Theory]
    [InlineData(1000, 18, 90, 90, 0, 180, 1180)]
    [InlineData(1000, 5, 25, 25, 0, 50, 1050)]
    [InlineData(1000, 12, 60, 60, 0, 120, 1120)]
    [InlineData(1000, 28, 140, 140, 0, 280, 1280)]
    [InlineData(500.50, 18, 45.05, 45.05, 0, 90.10, 590.60)]
    public void Calculate_IntraState_SplitsCgstSgst(
        decimal amount, decimal rate,
        decimal expectedCgst, decimal expectedSgst, decimal expectedIgst,
        decimal expectedTotalTax, decimal expectedTotal)
    {
        var result = _sut.Calculate(amount, rate, isIntraState: true);

        Assert.Equal(amount, result.BaseAmount);
        Assert.Equal(expectedCgst, result.CGST);
        Assert.Equal(expectedSgst, result.SGST);
        Assert.Equal(expectedIgst, result.IGST);
        Assert.Equal(expectedTotalTax, result.TotalTax);
        Assert.Equal(expectedTotal, result.TotalAmount);
    }

    // ── Inter-state (IGST) ─────────────────────────────────────────

    [Theory]
    [InlineData(1000, 18, 0, 0, 180, 180, 1180)]
    [InlineData(1000, 5, 0, 0, 50, 50, 1050)]
    [InlineData(1000, 28, 0, 0, 280, 280, 1280)]
    [InlineData(750.75, 12, 0, 0, 90.09, 90.09, 840.84)]
    public void Calculate_InterState_FullIgst(
        decimal amount, decimal rate,
        decimal expectedCgst, decimal expectedSgst, decimal expectedIgst,
        decimal expectedTotalTax, decimal expectedTotal)
    {
        var result = _sut.Calculate(amount, rate, isIntraState: false);

        Assert.Equal(amount, result.BaseAmount);
        Assert.Equal(expectedCgst, result.CGST);
        Assert.Equal(expectedSgst, result.SGST);
        Assert.Equal(expectedIgst, result.IGST);
        Assert.Equal(expectedTotalTax, result.TotalTax);
        Assert.Equal(expectedTotal, result.TotalAmount);
    }

    // ── Edge cases ─────────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroRate_ReturnsBaseOnly()
    {
        var result = _sut.Calculate(1000m, 0m, isIntraState: true);

        Assert.Equal(1000m, result.BaseAmount);
        Assert.Equal(0m, result.CGST);
        Assert.Equal(0m, result.SGST);
        Assert.Equal(0m, result.IGST);
        Assert.Equal(0m, result.TotalTax);
        Assert.Equal(1000m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_ZeroAmount_ReturnsZeros()
    {
        var result = _sut.Calculate(0m, 18m, isIntraState: true);

        Assert.Equal(0m, result.BaseAmount);
        Assert.Equal(0m, result.TotalTax);
        Assert.Equal(0m, result.TotalAmount);
    }

    // ── Rounding ───────────────────────────────────────────────────

    [Fact]
    public void Calculate_OddAmount_RoundsAwayFromZero()
    {
        // 999 × 9% = 89.91 each → CGST 44.955 rounds to 44.96
        var result = _sut.Calculate(999m, 9m, isIntraState: true);

        Assert.Equal(44.96m, result.CGST);
        Assert.Equal(44.96m, result.SGST);
        Assert.Equal(89.92m, result.TotalTax);
    }

    // ── Quantity overload ──────────────────────────────────────────

    [Fact]
    public void Calculate_WithQuantity_MultipliesBeforeTax()
    {
        var result = _sut.Calculate(unitPrice: 250m, quantity: 4, taxRate: 18m, isIntraState: true);

        Assert.Equal(1000m, result.BaseAmount);
        Assert.Equal(90m, result.CGST);
        Assert.Equal(90m, result.SGST);
        Assert.Equal(1180m, result.TotalAmount);
    }

    // ── Validation ─────────────────────────────────────────────────

    [Fact]
    public void Calculate_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(-100m, 18m, isIntraState: true));
    }

    [Fact]
    public void Calculate_RateAbove100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(1000m, 101m, isIntraState: true));
    }

    [Fact]
    public void Calculate_NegativeRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(1000m, -5m, isIntraState: true));
    }

    [Fact]
    public void Calculate_NegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(250m, -1, 18m, isIntraState: true));
    }
}
