using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class PricingCalculationServiceTests
{
    private readonly IPricingCalculationService _sut = new PricingCalculationService();

    // ── Tax-exclusive (standard retail) ────────────────────────────

    [Theory]
    [InlineData(100, 1, 18, 100, 18, 118)]
    [InlineData(100, 3, 18, 300, 54, 354)]
    [InlineData(250, 2, 5, 500, 25, 525)]
    [InlineData(999, 1, 12, 999, 119.88, 1118.88)]
    [InlineData(50, 10, 28, 500, 140, 640)]
    public void TaxExclusive_CalculatesCorrectly(
        decimal salePrice, int qty, decimal rate,
        decimal expectedSubtotal, decimal expectedTax, decimal expectedFinal)
    {
        var result = _sut.CalculateLineTotal(salePrice, qty, rate, isTaxInclusive: false);

        Assert.Equal(expectedSubtotal, result.Subtotal);
        Assert.Equal(expectedTax, result.TaxAmount);
        Assert.Equal(expectedFinal, result.FinalAmount);
    }

    // ── Tax-inclusive (MRP / shelf-price mode) ─────────────────────

    [Theory]
    [InlineData(118, 1, 18, 100, 18, 118)]
    [InlineData(354, 1, 18, 300, 54, 354)]
    [InlineData(525, 1, 5, 500, 25, 525)]
    public void TaxInclusive_BackCalculatesBase(
        decimal salePrice, int qty, decimal rate,
        decimal expectedSubtotal, decimal expectedTax, decimal expectedFinal)
    {
        var result = _sut.CalculateLineTotal(salePrice, qty, rate, isTaxInclusive: true);

        Assert.Equal(expectedSubtotal, result.Subtotal);
        Assert.Equal(expectedTax, result.TaxAmount);
        Assert.Equal(expectedFinal, result.FinalAmount);
    }

    [Fact]
    public void TaxInclusive_WithQuantity_BackCalculatesOnTotal()
    {
        // 2 × ₹118 = ₹236 inclusive of 18% GST
        // Base = 236 / 1.18 = 200.00, Tax = 36.00
        var result = _sut.CalculateLineTotal(118m, 2, 18m, isTaxInclusive: true);

        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(36m, result.TaxAmount);
        Assert.Equal(236m, result.FinalAmount);
    }

    // ── Zero-rate / exempt ─────────────────────────────────────────

    [Fact]
    public void ZeroRate_ReturnsSubtotalOnly()
    {
        var result = _sut.CalculateLineTotal(500m, 3, 0m, isTaxInclusive: false);

        Assert.Equal(1500m, result.Subtotal);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(1500m, result.FinalAmount);
    }

    [Fact]
    public void ZeroQuantity_ReturnsZeros()
    {
        var result = _sut.CalculateLineTotal(100m, 0, 18m, isTaxInclusive: false);

        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(0m, result.FinalAmount);
    }

    // ── Invariant: TaxInclusive FinalAmount = SalePrice × Qty ─────

    [Theory]
    [InlineData(99.99, 3, 18)]
    [InlineData(149.50, 7, 12)]
    [InlineData(1000, 1, 28)]
    public void TaxInclusive_FinalAmount_AlwaysEqualsShelfTotal(
        decimal salePrice, int qty, decimal rate)
    {
        var shelfTotal = decimal.Round(salePrice * qty, 2, MidpointRounding.AwayFromZero);
        var result = _sut.CalculateLineTotal(salePrice, qty, rate, isTaxInclusive: true);

        Assert.Equal(shelfTotal, result.FinalAmount);
    }

    // ── Invariant: Subtotal + TaxAmount = FinalAmount ─────────────

    [Theory]
    [InlineData(250, 4, 5, false)]
    [InlineData(250, 4, 5, true)]
    [InlineData(999.99, 1, 18, false)]
    [InlineData(999.99, 1, 18, true)]
    public void SubtotalPlusTax_AlwaysEqualsFinal(
        decimal salePrice, int qty, decimal rate, bool inclusive)
    {
        var result = _sut.CalculateLineTotal(salePrice, qty, rate, inclusive);

        Assert.Equal(result.FinalAmount, result.Subtotal + result.TaxAmount);
    }

    // ── Validation ─────────────────────────────────────────────────

    [Fact]
    public void NegativeSalePrice_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.CalculateLineTotal(-10m, 1, 18m, false));
    }

    [Fact]
    public void NegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.CalculateLineTotal(100m, -1, 18m, false));
    }

    [Fact]
    public void RateAbove100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.CalculateLineTotal(100m, 1, 101m, false));
    }

    [Fact]
    public void NegativeRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.CalculateLineTotal(100m, 1, -5m, false));
    }
}
