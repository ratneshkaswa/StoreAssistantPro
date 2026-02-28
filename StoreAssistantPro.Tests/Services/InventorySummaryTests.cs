using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="InventorySummary"/> record.
/// Feature #72 — Stock value report.
/// </summary>
public class InventorySummaryTests
{
    [Fact]
    public void Record_StoresAllFields()
    {
        var summary = new InventorySummary(
            TotalProducts: 100,
            ActiveProducts: 85,
            TotalUnits: 5000,
            TotalCostValue: 250_000m,
            TotalSaleValue: 400_000m,
            LowStockCount: 12,
            OutOfStockCount: 3);

        Assert.Equal(100, summary.TotalProducts);
        Assert.Equal(85, summary.ActiveProducts);
        Assert.Equal(5000, summary.TotalUnits);
        Assert.Equal(250_000m, summary.TotalCostValue);
        Assert.Equal(400_000m, summary.TotalSaleValue);
        Assert.Equal(12, summary.LowStockCount);
        Assert.Equal(3, summary.OutOfStockCount);
    }

    [Fact]
    public void EmptySummary_AllZeros()
    {
        var summary = new InventorySummary(0, 0, 0, 0m, 0m, 0, 0);

        Assert.Equal(0, summary.TotalProducts);
        Assert.Equal(0m, summary.TotalCostValue);
        Assert.Equal(0m, summary.TotalSaleValue);
    }

    [Fact]
    public void Margin_IsSaleMinusCost()
    {
        var summary = new InventorySummary(50, 50, 1000, 100_000m, 160_000m, 0, 0);

        var margin = summary.TotalSaleValue - summary.TotalCostValue;
        Assert.Equal(60_000m, margin);
    }

    [Fact]
    public void Record_SupportsEquality()
    {
        var a = new InventorySummary(10, 10, 100, 5000m, 8000m, 2, 1);
        var b = new InventorySummary(10, 10, 100, 5000m, 8000m, 2, 1);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_SupportsInequality()
    {
        var a = new InventorySummary(10, 10, 100, 5000m, 8000m, 2, 1);
        var b = new InventorySummary(10, 10, 100, 5000m, 9000m, 2, 1);

        Assert.NotEqual(a, b);
    }
}
