using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class BillCalculationServiceTests
{
    private readonly IBillCalculationService _sut = new BillCalculationService();

    // ── No discount ────────────────────────────────────────────────

    [Fact]
    public void NoDiscount_NullPassed_TaxAddedOnSubtotal()
    {
        var result = _sut.Calculate(1000m, 18m, null);

        Assert.Equal(1000m, result.Subtotal);
        Assert.Equal(DiscountType.None, result.DiscountType);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(1000m, result.TaxableAmount);
        Assert.Equal(180m, result.TaxAmount);
        Assert.Equal(1180m, result.FinalAmount);
    }

    [Fact]
    public void NoDiscount_ExplicitNone_SameAsNull()
    {
        var result = _sut.Calculate(1000m, 18m, BillDiscount.None);

        Assert.Equal(1000m, result.TaxableAmount);
        Assert.Equal(180m, result.TaxAmount);
        Assert.Equal(1180m, result.FinalAmount);
    }

    // ── Amount discount ────────────────────────────────────────────

    [Fact]
    public void AmountDiscount_SubtractedBeforeTax()
    {
        var discount = new BillDiscount
        {
            Type = DiscountType.Amount,
            Value = 200m,
            Reason = "Loyalty"
        };

        var result = _sut.Calculate(1000m, 18m, discount);

        Assert.Equal(1000m, result.Subtotal);
        Assert.Equal(DiscountType.Amount, result.DiscountType);
        Assert.Equal(200m, result.DiscountValue);
        Assert.Equal(200m, result.DiscountAmount);
        Assert.Equal(800m, result.TaxableAmount);
        Assert.Equal(144m, result.TaxAmount);        // 800 × 18%
        Assert.Equal(944m, result.FinalAmount);
    }

    [Fact]
    public void AmountDiscount_ExceedsSubtotal_CappedAtSubtotal()
    {
        var discount = new BillDiscount { Type = DiscountType.Amount, Value = 5000m };

        var result = _sut.Calculate(1000m, 18m, discount);

        Assert.Equal(1000m, result.DiscountAmount);   // capped
        Assert.Equal(0m, result.TaxableAmount);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(0m, result.FinalAmount);
    }

    // ── Percentage discount ────────────────────────────────────────

    [Fact]
    public void PercentageDiscount_AppliedBeforeTax()
    {
        var discount = new BillDiscount
        {
            Type = DiscountType.Percentage,
            Value = 10m   // 10%
        };

        var result = _sut.Calculate(1000m, 18m, discount);

        Assert.Equal(100m, result.DiscountAmount);     // 1000 × 10%
        Assert.Equal(900m, result.TaxableAmount);
        Assert.Equal(162m, result.TaxAmount);           // 900 × 18%
        Assert.Equal(1062m, result.FinalAmount);
    }

    [Fact]
    public void PercentageDiscount_100Percent_ZeroFinal()
    {
        var discount = new BillDiscount { Type = DiscountType.Percentage, Value = 100m };

        var result = _sut.Calculate(500m, 18m, discount);

        Assert.Equal(500m, result.DiscountAmount);
        Assert.Equal(0m, result.TaxableAmount);
        Assert.Equal(0m, result.FinalAmount);
    }

    [Fact]
    public void PercentageDiscount_Over100_Throws()
    {
        var discount = new BillDiscount { Type = DiscountType.Percentage, Value = 150m };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(1000m, 18m, discount));
    }

    // ── Zero tax ───────────────────────────────────────────────────

    [Fact]
    public void ZeroTax_DiscountStillApplied()
    {
        var discount = new BillDiscount { Type = DiscountType.Amount, Value = 50m };

        var result = _sut.Calculate(500m, 0m, discount);

        Assert.Equal(50m, result.DiscountAmount);
        Assert.Equal(450m, result.TaxableAmount);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(450m, result.FinalAmount);
    }

    // ── Rounding ───────────────────────────────────────────────────

    [Fact]
    public void PercentageDiscount_OddAmount_RoundsCorrectly()
    {
        // 999 × 7.5% = 74.925 → rounds to 74.93
        var discount = new BillDiscount { Type = DiscountType.Percentage, Value = 7.5m };

        var result = _sut.Calculate(999m, 18m, discount);

        Assert.Equal(74.93m, result.DiscountAmount);
        Assert.Equal(924.07m, result.TaxableAmount);
        Assert.Equal(166.33m, result.TaxAmount);  // 924.07 × 18% = 166.3326 → 166.33
        Assert.Equal(1090.40m, result.FinalAmount);
    }

    // ── Invariant: Subtotal − DiscountAmount = TaxableAmount ──────

    [Theory]
    [InlineData(1500, 18, 10)]
    [InlineData(750.50, 12, 25)]
    [InlineData(3000, 5, 50)]
    public void Invariant_TaxableAmount_EqualsSubtotalMinusDiscount(
        decimal subtotal, decimal rate, decimal discountPct)
    {
        var discount = new BillDiscount { Type = DiscountType.Percentage, Value = discountPct };
        var result = _sut.Calculate(subtotal, rate, discount);

        Assert.Equal(result.Subtotal - result.DiscountAmount, result.TaxableAmount);
    }

    // ── Invariant: TaxableAmount + TaxAmount = FinalAmount ────────

    [Theory]
    [InlineData(1000, 18, 200)]
    [InlineData(500, 5, 100)]
    [InlineData(2000, 28, 0)]
    public void Invariant_TaxablePlusTax_EqualsFinal(
        decimal subtotal, decimal rate, decimal discountAmt)
    {
        var discount = discountAmt > 0
            ? new BillDiscount { Type = DiscountType.Amount, Value = discountAmt }
            : BillDiscount.None;

        var result = _sut.Calculate(subtotal, rate, discount);

        Assert.Equal(result.TaxableAmount + result.TaxAmount, result.FinalAmount);
    }

    // ── Validation ─────────────────────────────────────────────────

    [Fact]
    public void NegativeSubtotal_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(-100m, 18m));
    }

    [Fact]
    public void NegativeRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(1000m, -5m));
    }

    [Fact]
    public void RateAbove100_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Calculate(1000m, 101m));
    }
}
